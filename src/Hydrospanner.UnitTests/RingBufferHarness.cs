namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using Disruptor;

	public class RingBufferHarness<T> : IRingBuffer<T> where T : class, new()
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

		public BatchDescriptor Next(int size)
		{
			for (var i = 0; i < size; i++)
				this.AllItems.Add(new T());

			this.index += size;

			return new BatchDescriptor(size) { End = this.index - 1 };
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

		public RingBufferHarness(Action callback = null)
		{
			this.callback = callback;
			this.AllItems = new List<T>();
			this.PublishedItems = new List<T>();
		}

		private readonly Action callback;
		private int index;
	}
}