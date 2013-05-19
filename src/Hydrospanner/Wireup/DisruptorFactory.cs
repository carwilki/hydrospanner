namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Persistence;
	using Phases;
	using Phases.Bootstrap;
	using Phases.Journal;
	using Phases.Snapshot;
	using Phases.Transformation;
	using Serialization;
	using Timeout;
	using SerializationHandler = Phases.Transformation.SerializationHandler;

	internal class DisruptorFactory
	{
		public virtual IDisruptor<BootstrapItem> CreateBootstrapDisruptor(IRepository repository, int countdown, Action<bool> complete)
		{
			var serializerCount = 1;
			if (countdown > 1024 * 4)
				serializerCount++;
			if (countdown > 1024 * 16)
				serializerCount++;
			if (countdown > 1024 * 64)
				serializerCount++;

			var serializers = new Phases.Bootstrap.SerializationHandler[serializerCount];
			for (var i = 0; i < serializerCount; i++)
				serializers[i] = new Phases.Bootstrap.SerializationHandler(this.CreateInboundSerializer(), serializerCount, i);

			var disruptor = CreateSingleThreadedDisruptor<BootstrapItem>(new SleepingWaitStrategy(), 1024 * 64);
			disruptor
				.HandleEventsWith(serializers.Cast<IEventHandler<BootstrapItem>>().ToArray())
				.Then(new MementoHandler(repository))
				.Then(new CountdownHandler(countdown, complete))
				.Then(new ClearItemHandler());

			return new DisruptorBase<BootstrapItem>(disruptor);
		}

		public virtual IDisruptor<JournalItem> CreateJournalDisruptor(BootstrapInfo info)
		{
			var messageStore = this.persistence.CreateMessageStore(info.SerializedTypes);
			var messageSender = this.messaging.CreateNewMessageSender();
			var checkpointStore = this.persistence.CreateDispatchCheckpointStore();

			var disruptor = CreateSingleThreadedDisruptor<JournalItem>(new SleepingWaitStrategy(), 1024 * 16);
			disruptor.HandleEventsWith(new Phases.Journal.SerializationHandler(CreateOutboundSerializer()))
				.Then(new JournalHandler(messageStore))
				.Then(new AcknowledgmentHandler(), new DispatchHandler(messageSender))
				.Then(new DispatchCheckpointHandler(checkpointStore))
				.Then(new ClearItemHandler());

			this.journalRing = new RingBufferBase<JournalItem>(disruptor.RingBuffer);
			return new DisruptorBase<JournalItem>(disruptor);
		}
		public virtual IDisruptor<SnapshotItem> CreateSnapshotDisruptor()
		{
			var systemRecorder = this.snapshots.CreateSystemSnapshotRecorder();
			var publicRecorder = this.snapshots.CreatePublicSnapshotRecorder();

			var systemHandler = new SystemSnapshotHandler(systemRecorder);
			var publicHandler = new PublicSnapshotHandler(publicRecorder);

			// FUTURE: this enables publishing the projections on the wire.
			// var dispatchHandler = new PublicSnapshotDispatchHandler(this.messaging.CreateNewMessageSender());

			var disruptor = CreateSingleThreadedDisruptor<SnapshotItem>(new SleepingWaitStrategy(), 1024 * 64);
			disruptor.HandleEventsWith(new Phases.Snapshot.SerializationHandler(this.CreateInboundSerializer()))
				.Then(systemHandler, publicHandler)
				.Then(new ClearItemHandler());

			this.snapshotRing = new RingBufferBase<SnapshotItem>(disruptor.RingBuffer);
			return new DisruptorBase<SnapshotItem>(disruptor);
		}

		public virtual IDisruptor<TransformationItem> CreateStartupTransformationDisruptor(IRepository repository, BootstrapInfo info, Action<bool> complete)
		{
			var countdown = info.JournaledSequence - info.SnapshotSequence;
			if (countdown == 0)
				return null;

			var serializerCount = 1;
			if (countdown > 1024 * 32)
				serializerCount++;
			if (countdown > 1024 * 512)
				serializerCount++;
			if (countdown > 1024 * 1024 * 4)
				serializerCount++;

			var replayTransientTypes = new HashSet<Type>();
			var serializers = new SerializationHandler[serializerCount];
			for (var i = 0; i < serializerCount; i++)
				serializers[i] = new SerializationHandler(this.CreateInboundSerializer(), replayTransientTypes, serializerCount, i);
			var transformationHandler = this.CreateTransformationHandler(repository, info.JournaledSequence);

			var slots = ComputeDisruptorSize(countdown);
			var disruptor = CreateSingleThreadedDisruptor<TransformationItem>(new SleepingWaitStrategy(), slots);
			disruptor.HandleEventsWith(serializers.Cast<IEventHandler<TransformationItem>>().ToArray())
				.Then(transformationHandler)
				.Then(new CountdownHandler(countdown, complete))
				.Then(new ClearItemHandler());

			return new DisruptorBase<TransformationItem>(disruptor);
		}
		private static int ComputeDisruptorSize(long size)
		{
			size--;
			size |= size >> 1;
			size |= size >> 2;
			size |= size >> 4;
			size |= size >> 8;
			size |= size >> 16;
			size++;

			if (size == 1)
				return 2;

			return size > (1024 * 1024) ? (1024 * 1024) : (int)size;
		}
		private TransformationHandler CreateTransformationHandler(IRepository repository, long sequence, ISystemSnapshotTracker tracker = null)
		{
			tracker = tracker ?? new NullSystemSnapshotTracker();
			var watcher = TimeoutHydratable.Load(repository);

			var transformer = (ITransformer)new Transformer(repository, this.snapshotRing, watcher);
			transformer = new CommandFilterTransformer(transformer);
			var handler = new ReflectionDeliveryHandler(transformer);
			return new TransformationHandler(sequence, this.journalRing, handler, tracker);
		}
		public virtual IDisruptor<TransformationItem> CreateTransformationDisruptor(IRepository repository, BootstrapInfo info)
		{
			var serializationHandler = new SerializationHandler(this.CreateInboundSerializer(), this.transientTypes);
			var systemSnapshotTracker = new SystemSnapshotTracker(info.JournaledSequence, this.snapshotFrequency, this.snapshotRing, repository);
			var transformationHandler = this.CreateTransformationHandler(repository, info.JournaledSequence, systemSnapshotTracker);

			var disruptor = CreateMultithreadedDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 32);
			disruptor
				.HandleEventsWith(serializationHandler)
				.Then(transformationHandler)
				.Then(new ClearItemHandler());

			return new DisruptorBase<TransformationItem>(disruptor);
		}

		private ISerializer CreateInboundSerializer()
		{
			return new JsonSerializer(this.aliasTypes);
		}
		private static ISerializer CreateOutboundSerializer()
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

		public DisruptorFactory(
			MessagingFactory messaging,
			PersistenceFactory persistence,
			SnapshotFactory snapshots,
			int snapshotFrequency,
			IDictionary<string, Type> aliasTypes,
			IEnumerable<Type> transientTypes)
		{
			if (messaging == null)
				throw new ArgumentNullException("messaging");

			if (persistence == null)
				throw new ArgumentNullException("persistence");

			if (snapshots == null)
				throw new ArgumentNullException("snapshots");

			if (snapshotFrequency <= 0)
				throw new ArgumentOutOfRangeException("snapshotFrequency");

			if (aliasTypes == null)
				throw new ArgumentNullException("aliasTypes");
			
			if (transientTypes == null)
				throw new ArgumentNullException("transientTypes");

			this.messaging = messaging;
			this.snapshots = snapshots;
			this.persistence = persistence;
			this.snapshotFrequency = snapshotFrequency;
			this.aliasTypes = aliasTypes;
			this.transientTypes = new HashSet<Type>(transientTypes);
		}
		protected DisruptorFactory()
		{
		}

		private readonly SnapshotFactory snapshots;
		private readonly MessagingFactory messaging;
		private readonly PersistenceFactory persistence;
		private readonly int snapshotFrequency;
		private readonly IDictionary<string, Type> aliasTypes;
		private readonly HashSet<Type> transientTypes;
		private IRingBuffer<JournalItem> journalRing;
		private IRingBuffer<SnapshotItem> snapshotRing;
	}
}
