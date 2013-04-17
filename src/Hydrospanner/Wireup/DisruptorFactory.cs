namespace Hydrospanner.Wireup
{
	using System;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Persistence;
	using Phases.Bootstrap;
	using Phases.Journal;
	using Phases.Snapshot;
	using Phases.Transformation;
	using Serialization;

	public class DisruptorFactory
	{
		public virtual IDisruptor<BootstrapItem> CreateBootstrapDisruptor(IRepository repository, int countdown, Action complete)
		{
			var disruptor = CreateDisruptor<BootstrapItem>(new YieldingWaitStrategy(), 1024 * 64);
			disruptor
				.HandleEventsWith(new Phases.Bootstrap.SerializationHandler(CreateSerializer()))
				.Then(new MementoHandler(repository))
				.Then(new CountdownHandler(countdown, complete));

			return new DisruptorBase<BootstrapItem>(disruptor);
		}

		public virtual IDisruptor<JournalItem> CreateJournalDisruptor(BootstrapInfo info)
		{
			var messageStore = this.persistence.CreateMessageStore(info.SerializedTypes);
			var messageSender = this.messaging.CreateMessageSender();
			var checkpointStore = this.persistence.CreateDispatchCheckpointStore();

			var disruptor = CreateDisruptor<JournalItem>(new SleepingWaitStrategy(), 1024 * 256);
			disruptor.HandleEventsWith(new Phases.Journal.SerializationHandler(new JsonSerializer()))
			    .Then(new JournalHandler(messageStore))
			    .Then(new AcknowledgmentHandler(), new DispatchHandler(messageSender))
			    .Then(new DispatchCheckpointHandler(checkpointStore));

			this.journalRing = new RingBufferBase<JournalItem>(disruptor.RingBuffer);
			return new DisruptorBase<JournalItem>(disruptor);
		}
		public virtual IDisruptor<SnapshotItem> CreateSnapshotDisruptor()
		{
			var systemRecorder = this.snapshots.CreateSystemSnapshotRecorder();
			var publicRecorder = this.snapshots.CreatePublicSnapshotRecorder();

			var disruptor = CreateDisruptor<SnapshotItem>(new SleepingWaitStrategy(), 1024 * 16);
			disruptor.HandleEventsWith(new Phases.Snapshot.SerializationHandler(CreateSerializer()))
			    .Then(new SystemSnapshotHandler(systemRecorder), new PublicSnapshotHandler(publicRecorder));

			this.snapshotRing = new RingBufferBase<SnapshotItem>(disruptor.RingBuffer);
			return new DisruptorBase<SnapshotItem>(disruptor);
		}

		public virtual IDisruptor<TransformationItem> CreateStartupTransformationDisruptor(
			IRepository repository, BootstrapInfo info, int snapshotFrequency, Action complete)
		{
			var duplicateHandler = new NullDuplicateHandler();
			var transformer = new Transformer(repository, this.snapshotRing, info.JournaledSequence);
			var systemSnapshotTracker = new SystemSnapshotTracker(
				info.JournaledSequence, snapshotFrequency, this.snapshotRing, repository);
			this.transformationHandler = new TransformationHandler(
				info.JournaledSequence, this.journalRing, duplicateHandler, transformer, systemSnapshotTracker);

			var countdown = info.JournaledSequence - info.SnapshotSequence;
			if (countdown == 0)
				return null;

			var deserializer1 = new DeserializationHandler(CreateSerializer(), 2, 0);
			var deserializer2 = new DeserializationHandler(CreateSerializer(), 2, 1);

			var disruptor = CreateDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 1024);
			disruptor.HandleEventsWith(deserializer1, deserializer2)
				.Then(this.transformationHandler)
				.Then(new CountdownHandler(countdown, complete));

			return new DisruptorBase<TransformationItem>(disruptor);
		}
		public virtual IDisruptor<TransformationItem> CreateTransformationDisruptor()
		{
			var disruptor = CreateDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 256);
			disruptor.HandleEventsWith(this.serializationHandler).Then(this.transformationHandler);
			return new DisruptorBase<TransformationItem>(disruptor);
		}

		private static ISerializer CreateSerializer()
		{
			return new JsonSerializer();
		}
		private static Disruptor<T> CreateDisruptor<T>(IWaitStrategy wait, int size) where T : class, new()
		{
			return new Disruptor<T>(() => new T(), new SingleThreadedClaimStrategy(size), wait, TaskScheduler.Default);
		}

		public DisruptorFactory(MessagingFactory messaging, PersistenceFactory persistence, SnapshotFactory snapshots)
		{
			this.messaging = messaging;
			this.snapshots = snapshots;
			this.persistence = persistence;
		}
		protected DisruptorFactory()
		{
		}

		private readonly DeserializationHandler serializationHandler = new DeserializationHandler(CreateSerializer());
		private readonly SnapshotFactory snapshots;
		private readonly MessagingFactory messaging;
		private readonly PersistenceFactory persistence;

		private TransformationHandler transformationHandler;
		private IRingBuffer<JournalItem> journalRing;
		private IRingBuffer<SnapshotItem> snapshotRing;
	}
}