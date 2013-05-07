namespace Hydrospanner
{
	using System;
	using Disruptor;
	using Disruptor.Dsl;
	
	internal interface IDisruptor<T> : IDisposable where T : class
	{
		IRingBuffer<T> RingBuffer { get; }
		IRingBuffer<T> Start();
		void Stop();
	}

	internal sealed class DisruptorBase<T> : IDisruptor<T> where T : class
	{
		public IRingBuffer<T> RingBuffer
		{
			get { return this.ring; }
		}

		public IRingBuffer<T> Start()
		{
			this.disruptor.Start();
			return this.ring;
		}
		public void Stop()
		{
			this.disruptor.Halt();
		}

		public DisruptorBase(Disruptor<T> disruptor)
		{
			this.disruptor = disruptor;
			this.ring = new RingBufferBase<T>(disruptor.RingBuffer);
		}
		public void Dispose()
		{
			this.Stop();
		}

		private readonly Disruptor<T> disruptor;
		private readonly RingBufferBase<T> ring; 
	}
}