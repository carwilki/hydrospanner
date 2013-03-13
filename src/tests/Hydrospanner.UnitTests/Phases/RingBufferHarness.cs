namespace Hydrospanner.Phases
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Disruptor;
	using Disruptor.Dsl;

	public class RingBufferHarness<T> : IDisposable, IEventHandler<T> where T : class, new()
	{
		public RingBuffer<T> RingBuffer { get; private set; }
		public List<T> AllItems { get; private set; }
		public List<T> CurrentBatch { get; private set; }

		public void OnNext(T data, long sequence, bool endOfBatch)
		{
			if (this.clearBatch)
				this.CurrentBatch.Clear();

			this.clearBatch = endOfBatch;
			this.AllItems.Add(data);
			this.CurrentBatch.Add(data);
		}

		public RingBufferHarness(int ringSize = 1024)
		{
			this.AllItems = new List<T>();
			this.CurrentBatch = new List<T>();

			this.disruptor = new Disruptor<T>(
				() => new T(),
				new SingleThreadedClaimStrategy(ringSize),
				new SleepingWaitStrategy(),
				TaskScheduler.Default);

			this.disruptor.HandleEventsWith(this);
			this.RingBuffer = this.disruptor.Start();
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			this.disruptor.Halt();
			this.disruptor.Shutdown();
			this.AllItems.Clear();
			this.CurrentBatch.Clear();
		}

		private readonly Disruptor<T> disruptor;
		private bool clearBatch;
	}
}