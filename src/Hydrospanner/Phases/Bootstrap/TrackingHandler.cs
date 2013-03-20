namespace Hydrospanner.Phases.Bootstrap
{
	using Disruptor;
	using Disruptor.Dsl;

	public class TrackingHandler : IEventHandler<BootstrapItem>
	{
		public void OnNext(BootstrapItem data, long sequence, bool endOfBatch)
		{
			if (--this.countdown > 0)
				return;

//			this.disruptor.Shutdown();
		}

		public void Expect(int messages)
		{
			this.countdown = messages;
		}

		int countdown;

		public TrackingHandler(Disruptor<BootstrapItem> disruptor)
		{
			this.disruptor = disruptor;
		}

		readonly Disruptor<BootstrapItem> disruptor;
	}
}