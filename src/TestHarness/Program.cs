namespace TestHarness
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Accounting.Events;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner;
	using Hydrospanner.Inbox;

	internal static class Program
	{
		private static void Main()
		{
			////var identifiers = new Dictionary<Type, IStreamIdentifier>();
			////identifiers[typeof(AccountClosedEvent)] = new TestStreamIdentifier();

			////var phase3Disruptor = BuildDisruptor<DispatchMessage>();
			////phase3Disruptor
			////	.HandleEventsWith(new SerializationHandler())
			////	.Then(new JournalHandler())
			////	.Then(new DispatchHandler(), new AcknowledgementHandler()); // TODO: ack delivery to storage
			////var phase3RingBuffer = phase3Disruptor.Start();

			////var phase2Disruptor = BuildDisruptor<ParsedMessage>();
			////phase2Disruptor
			////	.HandleEventsWith(new JournaledDeserializationHandler())
			////	.Then(new TransformationHandler())
			////	.Then(new ReplicationHandler(phase3RingBuffer));
			////var phase2RingBuffer = phase2Disruptor.Start();

			var identifierTable = new StreamIdentifierRoutingTable();
			identifierTable.Register(new TestStreamIdentifier());

			var phase1Disruptor = BuildDisruptor<WireMessage>();
			phase1Disruptor
				.HandleEventsWith(new DeserializationHandler())
				.Then(new JournalHandler("Hydrospanner", identifierTable))
				.Then(new RepositoryHandler())
				.Then(new AcknowledgementHandler());

			using (var listener = new MessageListener(phase1Disruptor.Start()))
			{
				listener.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
				listener.Stop();
			}
		}
		private static List<IHydratable> Build(Guid streamId)
		{
			return new List<IHydratable>(new[] { new TestHydratable() });
		}

		private static Disruptor<T> BuildDisruptor<T>() where T : class, new()
		{
			return new Disruptor<T>(
				() => new T(),
				new SingleThreadedClaimStrategy(PreallocatedSize), 
				new SleepingWaitStrategy(),
				TaskScheduler.Default);
		}

		private const int PreallocatedSize = 1024 * 16;
	}

	public class TestStreamIdentifier : IStreamIdentifier<AccountClosedEvent>
	{
		public Guid DiscoverStreams(AccountClosedEvent message, Hashtable headers)
		{
			return message.AccountId;
		}
	}
	public class TestHydratable : IHydratable
	{
		public void Hydrate(object message, Hashtable headers)
		{
			// provide to underlying aggregate/saga/projector
		}

		public IEnumerable<object> GatherMessages()
		{
			return new object[]
			{
				new AccountClosedEvent
				{
					AccountId = Guid.NewGuid(),
					Description = "Hello, World!",
					Dispatched = DateTime.UtcNow,
					MessageId = Guid.NewGuid(),
					Reason = CloseReason.Abuse,
					UserId = Guid.NewGuid(),
					Username = "test@test.com"
				}
			};
		}
	}
}