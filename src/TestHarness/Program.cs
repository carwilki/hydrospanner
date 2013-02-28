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
			var disruptor = BuildDisruptor();
			disruptor
				.HandleEventsWith(new DeserializationHandler())
				.Then(new AcknowledgementHandler());

			using (var listener = new MessageListener(disruptor.Start()))
			{
				listener.Start();
				Console.WriteLine("Press enter");
				Console.ReadLine();
				listener.Stop();
			}
		}
		private static Disruptor<ReceivedMessage> BuildDisruptor()
		{
			return new Disruptor<ReceivedMessage>(
				() => new ReceivedMessage(),
				new SingleThreadedClaimStrategy(PreallocatedSize), 
				new SleepingWaitStrategy(),
				TaskScheduler.Default);
		}

		private const int PreallocatedSize = 1024 * 4;
	}
}