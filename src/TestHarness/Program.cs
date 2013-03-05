namespace TestHarness
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner;
	using Hydrospanner.Inbox;
    using Hydrospanner.Outbox;
    using Hydrospanner.Transformation;

	internal static class Program
	{
		private static void Main()
		{
		    var inboxPhase = ConfigurePhases();

		    using (var listener = new MessageListener(inboxPhase.Start()))
			{
				listener.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
				listener.Stop();
			}
		}

	    private static Disruptor<WireMessage> ConfigurePhases()
	    {
            var identifierTable = new StreamIdentifierRoutingTable();
            identifierTable.Register(new TestStreamIdentifier());

	        var outboxPhase = BuildDisruptor<DispatchMessage>();
	        outboxPhase
	            .HandleEventsWith(new SerializationHandler())
	            .Then(new DispatchHandler())
	            .Then(new BookmarkHandler(ConnectionName));
	            // TODO: need handler to put newly generated events back into the hydrospanner

	        var transformationPhase = BuildDisruptor<TransformationMessage>();
	        transformationPhase
	            .HandleEventsWith(new TransformationDeserializationHandler())
	            .Then(new TransformationHandler(outboxPhase.Start()));

	        var inboxPhase = BuildDisruptor<WireMessage>();
	        inboxPhase
	            .HandleEventsWith(new InboxDeserializationHandler())
	            .Then(new JournalHandler(ConnectionName, identifierTable))
	            .Then(new RepositoryHandler(ConnectionName, BuildHydratables, transformationPhase.Start()))
	            .Then(new AcknowledgementHandler());
	        
            return inboxPhase;
	    }

	    private static List<IHydratable> BuildHydratables()
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
	    private const string ConnectionName = "Hydrospanner";
	}
}