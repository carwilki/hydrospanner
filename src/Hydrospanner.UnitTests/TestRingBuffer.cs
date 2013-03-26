namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public class TestRingBuffer<T> : IRingBuffer<T> where T : class, new()
	{
		public List<T> AllItems { get; private set; }
		public List<T> PublishedItems { get; private set; }

		public long Next()
		{
			this.AllItems.Add(new T());
			return this.index++;
		}

		public T this[long sequence]
		{
			get { return this.AllItems[(int)sequence]; }
		}

		public void Publish(long sequence)
		{
			this.Publish((int)sequence);
		}

		public BatchDescriptor NewBatchDescriptor(int size)
		{
			for (var i = 0; i < size; i++)
				this.AllItems.Add(new T());

			this.index += size;

			return new BatchDescriptor(size);
		}

		public void Publish(BatchDescriptor batch)
		{
			for (var i = this.index - batch.Size; i < batch.End; i++)
				this.Publish(i);
		}

		private void Publish(int i)
		{
			this.PublishedItems.Add(this.AllItems[i]);
			
			if (this.callback != null)
				this.callback();
		}

		public TestRingBuffer(Action callback = null)
		{
			this.callback = callback;
			this.AllItems = new List<T>();
			this.PublishedItems = new List<T>();
		}

		private readonly Action callback;
		private int index;
	}

	public class TestDisruptor<T> : IDisruptor<T> where T : class, new()
	{
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

		public TestDisruptor()
		{
			this.RingBuffer = new TestRingBuffer<T>();
		}

		public void Dispose()
		{
			this.Disposed = true;
		}

		public bool Started { get; private set; }
		public bool Stopped { get; private set; }
		public bool Disposed { get; private set; }
	}
}