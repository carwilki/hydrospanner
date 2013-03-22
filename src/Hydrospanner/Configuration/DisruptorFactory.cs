namespace Hydrospanner.Configuration
{
	using System;
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

	public class DisruptorFactory
	{
		public virtual IDisruptor<BootstrapItem> CreateBootstrapDisruptor(IRepository repository, int countdown, Action complete)
		{
			var disruptor = CreateDisruptor<BootstrapItem>(new YieldingWaitStrategy(), 1024);
			disruptor
				.HandleEventsWith(new Phases.Bootstrap.SerializationHandler(CreateSerializer()))
				.Then(new MementoHandler(repository))
				.Then(new CoutdownHandler(countdown, complete));

			return new DisruptorBase<BootstrapItem>(disruptor);
		}
		public virtual IDisruptor<TransformationItem> CreateStartupTransformationDisruptor(BootstrapInfo info, IRepository repository, Action complete)
		{
			var countdown = info.JournaledSequence - info.SnapshotSequence;
			var transformationHandler = new TransformationHandler(); // TODO

			var disruptor = CreateDisruptor<TransformationItem>(new YieldingWaitStrategy(), 1024 * 128);
			disruptor.HandleEventsWith(new DeserializationHandler(CreateSerializer()))
			    .Then(transformationHandler)
			    .Then(new CoutdownHandler(countdown, complete));

			return new DisruptorBase<TransformationItem>(disruptor);
		}

		public virtual IDisruptor<JournalItem> CreateJournalDisruptor(BootstrapInfo info, MessagingFactory messaging, PersistenceFactory persistence)
		{
			var messageStore = persistence.CreateMessageStore(info.SerializedTypes);
			var messageSender = messaging.CreateMessageSender();
			var checkpointStore = persistence.CreateDispatchCheckpointStore();

			var disruptor = CreateDisruptor<JournalItem>(new SleepingWaitStrategy(), 1024 * 64);
			disruptor.HandleEventsWith(new Phases.Journal.SerializationHandler(new JsonSerializer()))
			    .Then(new JournalHandler(messageStore))
			    .Then(new AcknowledgmentHandler(), new DispatchHandler(messageSender))
			    .Then(new DispatchCheckpointHandler(checkpointStore));

			return new DisruptorBase<JournalItem>(disruptor);
		}
		public virtual IDisruptor<SnapshotItem> CreateSnapshotDisruptor(SnapshotFactory factory)
		{
			var systemRecorder = factory.CreateSystemSnapshotRecorder();
			var publicRecorder = factory.CreatePublicSnapshotRecorder();

			var disruptor = CreateDisruptor<SnapshotItem>(new SleepingWaitStrategy(), 1024 * 8);
			disruptor.HandleEventsWith(new Phases.Snapshot.SerializationHandler(CreateSerializer()))
			    .Then(new SystemSnapshotHandler(systemRecorder, factory.SnapshotGeneration), new PublicSnapshotHandler(publicRecorder));

			return new DisruptorBase<SnapshotItem>(disruptor);
		}
		public virtual IDisruptor<TransformationItem> CreateTransformationDisruptor()
		{
			var transformationHandler = new TransformationHandler(); // TODO
			var disruptor = CreateDisruptor<TransformationItem>(new SleepingWaitStrategy(), 1024 * 128);
			disruptor.HandleEventsWith(new DeserializationHandler(CreateSerializer())).Then(transformationHandler);
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
	}
}