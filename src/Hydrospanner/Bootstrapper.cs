namespace Hydrospanner
{
	using System;
	using System.Configuration;
	using System.Threading;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;

	public class Bootstrapper : IDisposable
	{
		public void Start()
		{
			if (this.started)
				return;

			this.started = true;

			new Thread(() =>
			{
				var keys = this.storage.LoadWireIdentifiers(MaxDuplicates);
				foreach (var item in keys)
					this.duplicates.Contains(item);

				var maxSequence = this.storage.LoadMaxSequence();
				var transformationCheckpoint = this.storage.LoadTransformationCheckpoint();
				var dispatchCheckpoint = this.storage.LoadTransformationCheckpoint();
				var minCheckpoint = Math.Min(transformationCheckpoint, dispatchCheckpoint);

				var repo = new RepositoryHandler(this.transformationDisruptor.RingBuffer, this.settings.Name, transformationCheckpoint, factory);

				this.journalDisruptor
				    .HandleEventsWith(new SerializationHandler())
				    .Then(new IdentificationHandler(identifier, this.duplicates))
				    .Then(new JournalHandler(this.settings.Name, maxSequence))
				    .Then(repo);
					////.Then(new ForwardToDispatchHandler(this.dispatchDisruptor.RingBuffer), repo, new AcknowledgementHandler());

				////this.dispatchDisruptor
				////	.HandleEventsWith(new DispatchHandler(dispatchCheckpoint)); // TODO: update dispatch checkpoint

				this.transformationDisruptor
					.HandleEventsWith(new SerializationHandler())
					.Then(new TransformationHandler(this.journalDisruptor.RingBuffer));

				////this.dispatchDisruptor.Start();
				this.transformationDisruptor.Start();
				var ring = this.journalDisruptor.Start();

				var outstanding = this.storage.LoadSinceCheckpoint(minCheckpoint);
				foreach (var message in outstanding)
					PublishToRing(ring, message);

				this.listener.Start();
			}).Start();
		}
		private static void PublishToRing(RingBuffer<WireMessage> ring, JournaledMessage journaled)
		{
			var claimed = ring.Next();
			var message = ring[claimed];
			message.Clear();

			message.MessageSequence = journaled.Sequence;
			message.WireId = journaled.WireId;
			message.SerializedBody = journaled.SerializedBody;
			message.SerializedHeaders = journaled.SerializedHeaders;

			ring.Publish(claimed);
		}

		public Bootstrapper(IStreamIdentifier identifier, string connectionName, Func<Guid, IHydratable[]> factory)
		{
			this.identifier = identifier;
			this.factory = factory;
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];

			this.storage = new MessageStore(this.settings);
			this.duplicates = new DuplicateStore(MaxDuplicates);

			this.journalDisruptor = BuildDisruptor<WireMessage>(new MultiThreadedLowContentionClaimStrategy(PreallocatedSize));
			this.transformationDisruptor = BuildDisruptor<TransformationMessage>(new SingleThreadedClaimStrategy(PreallocatedSize));
			////this.dispatchDisruptor = BuildDisruptor<DispatchMessage>();
			this.listener = new MessageListener(this.journalDisruptor.RingBuffer);
		}
		private static Disruptor<T> BuildDisruptor<T>(IClaimStrategy strategy) where T : class, new()
		{
			return new Disruptor<T>(
				() => new T(),
				strategy,
				new YieldingWaitStrategy(), // different strategies drastically affect latency
				TaskScheduler.Default);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			if (!this.started)
				return;

			this.started = false;
			this.listener.Stop();
			//// Thread.Sleep(TimeSpan.FromSeconds(2)); // TODO: optimize this
			this.journalDisruptor.Shutdown();
			this.transformationDisruptor.Shutdown();
			////this.dispatchDisruptor.Shutdown();
		}

		private const int MaxDuplicates = 1024 * 64;
		private const int PreallocatedSize = 1024 * 64;
		private readonly ConnectionStringSettings settings;
		private readonly MessageStore storage;
		private readonly DuplicateStore duplicates;
		private readonly Disruptor<WireMessage> journalDisruptor;
		private readonly Disruptor<TransformationMessage> transformationDisruptor;
		////private readonly Disruptor<DispatchMessage> dispatchDisruptor;
		private readonly MessageListener listener;
		private readonly IStreamIdentifier identifier;
		private readonly Func<Guid, IHydratable[]> factory;
		private bool started;
	}
}