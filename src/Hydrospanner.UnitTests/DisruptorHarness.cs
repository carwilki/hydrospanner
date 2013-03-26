namespace Hydrospanner
{
	using System;

	public class DisruptorHarness<T> : IDisruptor<T> where T : class, new()
	{
		public bool Started { get; private set; }
		public bool Stopped { get; private set; }
		public bool Disposed { get; private set; }
		public RingBufferHarness<T> Ring { get { return (RingBufferHarness<T>)this.RingBuffer; } } 
		
		public IRingBuffer<T> RingBuffer { get; private set; }

		public IRingBuffer<T> Start()
		{
			this.Started = true;
			return this.RingBuffer;
		}

		public void Stop()
		{
			this.Stopped = true;
		}

		public DisruptorHarness(Action callback = null)
		{
			this.RingBuffer = new RingBufferHarness<T>(callback);
		}

		public void Dispose()
		{
			this.Disposed = true;
		}
	}
}