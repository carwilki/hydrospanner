namespace TestHarness
{
	using System;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;
	using Hydrospanner.Inbox2;

	internal static class Program
	{
		private static void Main()
		{
		    var inboxPhase = ConfigurePhases();

		    using (var listener = new MessageListener2(inboxPhase.Start()))
			{
				listener.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
				listener.Stop();
			}
		}

	    private static Disruptor<WireMessage2> ConfigurePhases()
	    {
	        var inboxPhase = BuildDisruptor<WireMessage2>();
	        inboxPhase
	            .HandleEventsWith(new SerializationHandler2())
	            .Then(new JournalHandler2(ConnectionName, new TestStreamIdentifier()))
	            .Then(new AcknowledgementHandler2());
	        
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