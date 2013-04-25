﻿namespace Hydrospanner.Wireup
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
	using Timeout;

	internal class DisruptorFactory
	{
		public virtual IDisruptor<BootstrapItem> CreateBootstrapDisruptor(IRepository repository, int countdown, Action<bool> complete)
		{
			var disruptor = CreateSingleThreadedDisruptor<BootstrapItem>(new YieldingWaitStrategy(), 1024 * 64);
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

			var disruptor = CreateSingleThreadedDisruptor<JournalItem>(new SleepingWaitStrategy(), 1024 * 256);
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

			var disruptor = CreateSingleThreadedDisruptor<SnapshotItem>(new SleepingWaitStrategy(), 1024 * 16);
			disruptor.HandleEventsWith(new Phases.Snapshot.SerializationHandler(CreateSerializer()))
			    .Then(new SystemSnapshotHandler(systemRecorder), new PublicSnapshotHandler(publicRecorder));

			this.snapshotRing = new RingBufferBase<SnapshotItem>(disruptor.RingBuffer);
			return new DisruptorBase<SnapshotItem>(disruptor);
		}

		public virtual IDisruptor<TransformationItem> CreateStartupTransformationDisruptor(IRepository repository, BootstrapInfo info, Action<bool> complete)
		{
			var countdown = info.JournaledSequence - info.SnapshotSequence;
			if (countdown == 0)
				return null;

			var deserializationHandler1 = new DeserializationHandler(CreateSerializer(), 2, 0);
			var deserializationHandler2 = new DeserializationHandler(CreateSerializer(), 2, 1);
			var transformationHandler = this.CreateTransformationHandler(repository, info.JournaledSequence);

			var disruptor = CreateSingleThreadedDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 1024);
			disruptor.HandleEventsWith(deserializationHandler1, deserializationHandler2)
				.Then(transformationHandler)
				.Then(new CountdownHandler(countdown, complete));

			return new DisruptorBase<TransformationItem>(disruptor);
		}
		private TransformationHandler CreateTransformationHandler(IRepository repository, long sequence, ISystemSnapshotTracker tracker = null, ITimeoutWatcher watcher = null)
		{
			watcher = watcher ?? new NullTimeoutWatcher(); // HUGE TODO: during re-build, the timeout watcher *does* need to pay attention to this stuff
			tracker = tracker ?? new NullSystemSnapshotTracker();

			var transformer = (ITransformer)new Transformer(repository, this.snapshotRing, watcher);
			transformer = new CommandFilterTransformer(transformer);
			var handler = new ReflectionDeliveryHandler(transformer);
			return new TransformationHandler(sequence, this.journalRing, handler, tracker);
		}
		public virtual IDisruptor<TransformationItem> CreateTransformationDisruptor(IRepository repository, BootstrapInfo info)
		{
			var watcher = TimeoutHydratable.Load(repository);
			var serializationHandler = new DeserializationHandler(CreateSerializer());
			var systemSnapshotTracker = new SystemSnapshotTracker(info.JournaledSequence, this.snapshotFrequency, this.snapshotRing, repository);
			var transformationHandler = this.CreateTransformationHandler(repository, info.JournaledSequence, systemSnapshotTracker, watcher);
			var disruptor = CreateMultithreadedDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 256);
			disruptor.HandleEventsWith(serializationHandler).Then(transformationHandler);
			return new DisruptorBase<TransformationItem>(disruptor);
		}

		private static ISerializer CreateSerializer()
		{
			return new JsonSerializer();
		}
		private static Disruptor<T> CreateSingleThreadedDisruptor<T>(IWaitStrategy wait, int size) where T : class, new()
		{
			return new Disruptor<T>(() => new T(), new SingleThreadedClaimStrategy(size), wait, TaskScheduler.Default);
		}
		private static Disruptor<T> CreateMultithreadedDisruptor<T>(IWaitStrategy wait, int size) where T : class, new()
		{
			return new Disruptor<T>(() => new T(), new MultiThreadedLowContentionClaimStrategy(size), wait, TaskScheduler.Default);
		}

		public DisruptorFactory(MessagingFactory messaging, PersistenceFactory persistence, SnapshotFactory snapshots, int snapshotFrequency)
		{
			if (messaging == null)
				throw new ArgumentNullException("messaging");

			if (persistence == null)
				throw new ArgumentNullException("persistence");

			if (snapshots == null)
				throw new ArgumentNullException("snapshots");

			if (snapshotFrequency <= 0)
				throw new ArgumentOutOfRangeException("snapshotFrequency");

			this.messaging = messaging;
			this.snapshots = snapshots;
			this.persistence = persistence;
			this.snapshotFrequency = snapshotFrequency;
		}
		protected DisruptorFactory()
		{
		}

		private readonly SnapshotFactory snapshots;
		private readonly MessagingFactory messaging;
		private readonly PersistenceFactory persistence;
		readonly int snapshotFrequency;

		private IRingBuffer<JournalItem> journalRing;
		private IRingBuffer<SnapshotItem> snapshotRing;
	}
}