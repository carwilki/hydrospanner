namespace TestHarness
{
	using System;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner;

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
	        var inboxPhase = BuildDisruptor<WireMessage>();
	        inboxPhase
	            .HandleEventsWith(new SerializationHandler())
				.Then(new IdentificationHandler(new TestStreamIdentifier(), new DuplicateStore(10000)))
	            .Then(new JournalHandler(ConnectionName))
	            .Then(new AcknowledgementHandler());
	        
            return inboxPhase;
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