namespace Hydrospanner.Phases.Bootstrap
{
	using System;
	using System.Configuration;
	using System.Data.Common;
	using System.IO.Abstractions;
	using System.Threading;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner.Messaging.Rabbit;
	using Hydrospanner.Persistence;
	using Hydrospanner.Persistence.SqlPersistence;
	using Hydrospanner.Phases.Journal;
	using Hydrospanner.Phases.Snapshot;
	using Hydrospanner.Phases.Transformation;
	using Hydrospanner.Serialization;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
			if (this.started || this.disposed)
				return;

			var bootstrapStore = new SqlBootstrapStore(this.factory, this.connectionString, MaxDuplicates);
			var info = bootstrapStore.Load();
			this.messageStore = new SqlMessageStore(this.factory, this.connectionString, info.SerializedTypes);

			info = this.LoadSnapshots(info);
			this.StartSnapshotRing();
			this.StartJournalRing();
			this.LoadMessages(info);
		}

		private BootstrapInfo LoadSnapshots(BootstrapInfo info)
		{
			if (info.JournaledSequence == 0)
				return info;

			var loader = new SystemSnapshotLoader(new DirectoryWrapper(), new FileWrapper(), "TODO-path-to-snapshot-files"); // TODO
			var reader = loader.Load(info.JournaledSequence, this.generation);
			if (reader.Count > 0 && reader.MessageSequence > 0)
				this.LoadSnapshots(reader);

			return info.AddSnapshotSequence(reader.MessageSequence);
		}
		private void LoadSnapshots(SystemSnapshotStreamReader reader)
		{
			var disruptor = this.CreateBootstrapDisruptor(reader.Count);
			var ring = disruptor.Start();
			foreach (var memento in reader.Read())
			{
				var claimed = ring.Next();
				var item = ring[claimed];
				item.AsSnapshot(memento.Key, memento.Value);
				ring.Publish(claimed);
			}

			this.mutex.WaitOne();
			disruptor.Shutdown();
		}
		private Disruptor<BootstrapItem> CreateBootstrapDisruptor(long countdown)
		{
			var disruptor = CreateDisruptor<BootstrapItem>(new YieldingWaitStrategy(), 1024);
			disruptor
				.HandleEventsWith(new SerializationHandler(new JsonSerializer()))
				.Then(new MementoHandler(this.repository))
				.Then(new CoutdownHandler(countdown, () => this.mutex.Set()));
			return disruptor;
		}

		private void StartSnapshotRing()
		{
			var systemRecorder = new SystemSnapshotRecorder(new FileWrapper(), "TODO--path-to-snapshot-files"); // TODO
			var publicRecorder = new PublicSnapshotRecorder(null); // TODO

			this.snapshotDisruptor = CreateDisruptor<SnapshotItem>(new SleepingWaitStrategy(), 1024 * 8);
			this.snapshotDisruptor.HandleEventsWith(new Snapshot.SerializationHandler(new JsonSerializer()))
			    .Then(new SystemSnapshotHandler(systemRecorder, this.generation), new PublicSnapshotHandler(publicRecorder));

			this.snapshotDisruptor.Start();
		}
		private void StartJournalRing()
		{
			this.journalDisruptor = CreateDisruptor<JournalItem>(new SleepingWaitStrategy(), 1024 * 64);
			this.journalDisruptor.HandleEventsWith(new Journal.SerializationHandler(new JsonSerializer()))
			    .Then(new JournalHandler(this.messageStore))
			    .Then(new AcknowledgmentHandler(), new DispatchHandler(this.messageSender))
			    .Then(new DispatchCheckpointHandler(this.checkpointStore));

			this.journalDisruptor.Start();
		}

		private void LoadMessages(BootstrapInfo info)
		{
			if (info.JournaledSequence == 0)
				return;

			var transformationHandler = new TransformationHandler(); // this will want access to the snapshot and journal rings
			var serializationHandler = new DeserializationHandler(new JsonSerializer());

			var countdown = info.JournaledSequence - info.SnapshotSequence;
			this.transformationDisruptor = CreateDisruptor<TransformationItem>(new YieldingWaitStrategy(), 1024 * 128);
			this.transformationDisruptor.HandleEventsWith(serializationHandler)
			    .Then(transformationHandler)
			    .Then(new CoutdownHandler(countdown, () => this.mutex.Set()));

			var transformationRing = this.transformationDisruptor.Start();
			var journalRing = this.journalDisruptor.RingBuffer;

			var startingSequence = Math.Min(info.SnapshotSequence + 1, info.DispatchSequence + 1);
			foreach (var message in this.messageStore.Load(startingSequence))
			{
				if (message.Sequence > info.DispatchSequence)
					PublishToJournalRing(journalRing, message);

				if (message.Sequence > info.SnapshotSequence)
					PublishToTransformationRing(transformationRing, message);
			}

			this.mutex.WaitOne();
			this.transformationDisruptor.Shutdown();

			this.transformationDisruptor = CreateDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 32);
			this.transformationDisruptor.HandleEventsWith(serializationHandler).Then(transformationHandler);
			this.transformationDisruptor.Start();
		}
		private static void PublishToTransformationRing(RingBuffer<TransformationItem> ring, JournaledMessage message)
		{
			var claimed = ring.Next();
			var item = ring[claimed];
			item.AsJournaledMessage(message.Sequence, message.SerializedBody, message.SerializedType, message.SerializedHeaders);
			ring.Publish(claimed);
		}
		private static void PublishToJournalRing(RingBuffer<JournalItem> ring, JournaledMessage message)
		{
			var claimed = ring.Next();
			var item = ring[claimed];
			item.AsBootstrappedDispatchMessage(message.Sequence, message.SerializedBody, message.SerializedType, message.SerializedHeaders);
			ring.Publish(claimed);
		}

		private static Disruptor<T> CreateDisruptor<T>(IWaitStrategy wait, int size) where T : class, new()
		{
			return new Disruptor<T>(
				() => new T(),
				new SingleThreadedClaimStrategy(size),
				wait,
				TaskScheduler.Default);
		}

		public Bootstrapper(IRepository repository, int generation, string connectionName = DefaultConnection)
		{
			if (repository == null)
				throw new ArgumentNullException("repository");

			if (generation < 0)
				throw new ArgumentOutOfRangeException("generation");

			if (string.IsNullOrWhiteSpace(connectionName))
				throw new ArgumentNullException("connectionName");

			var settings = ConfigurationManager.ConnectionStrings[connectionName];
			if (settings == null)
				throw new ConfigurationErrorsException("Unable to find connection named '{0}' in the configuration file.".FormatWith(connectionName));

			this.repository = repository;
			this.generation = generation;
			this.factory = DbProviderFactories.GetFactory(settings.ProviderName ?? DefaultProvider);
			this.connectionString = settings.ConnectionString;
			
			this.checkpointStore = new SqlCheckpointStore(this.factory, this.connectionString);
			this.messageSender = new RabbitChannel(this.rabbitConnector, 0); // TODO
			this.messageReceiver = new RabbitChannel(this.rabbitConnector, 0, x => new RabbitSubscription(x, "queue-name")); // TODO
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || this.disposed)
				return;

			this.started = false;
			this.disposed = true;

			this.messageReceiver.Dispose();
			TimeSpan.FromSeconds(3).Sleep();
			this.transformationDisruptor.Shutdown();
			TimeSpan.FromMilliseconds(500).Sleep();
			this.snapshotDisruptor.Shutdown();
			this.journalDisruptor.Shutdown();
			this.messageSender.Dispose();
			this.rabbitConnector.Dispose();
		}

		private const int MaxDuplicates = 1024 * 64;
		private const string DefaultConnection = "hydrospanner";
		private const string DefaultProvider = "System.Data.SqlClient";
		private readonly AutoResetEvent mutex = new AutoResetEvent(false);
		private readonly IRepository repository;
		private readonly int generation;
		private readonly DbProviderFactory factory;
		private readonly string connectionString;

		private readonly RabbitConnector rabbitConnector = new RabbitConnector(null); // TODO
		private readonly RabbitChannel messageSender;
		private readonly RabbitChannel messageReceiver;

		private readonly IDispatchCheckpointStore checkpointStore;
		private IMessageStore messageStore;

		private Disruptor<TransformationItem> transformationDisruptor;
		private Disruptor<SnapshotItem> snapshotDisruptor;
		private Disruptor<JournalItem> journalDisruptor;

		private bool started;
		private bool disposed;
	}
}