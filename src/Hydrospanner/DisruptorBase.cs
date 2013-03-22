namespace Hydrospanner
{
	using Disruptor;
	using Disruptor.Dsl;
	using Phases;

	public sealed class DisruptorBase<T> : IDisruptor<T> where T : class
	{
		public RingBuffer<T> RingBuffer
		{
			get { return this.disruptor.RingBuffer; }
		}

		public RingBuffer<T> Start()
		{
			return this.disruptor.Start();
		}
		public void Stop()
		{
			this.disruptor.Shutdown();
		}

		public DisruptorBase(Disruptor<T> disruptor)
		{
			this.disruptor = disruptor;
		}
		public void Dispose()
		{
			this.Stop();
		}

		private readonly Disruptor<T> disruptor;
	}
}