﻿namespace Hydrospanner
{
	using Disruptor;

	public interface IRingBuffer<T> where T : class
	{
		T this[long sequence] { get; }

		long Next();
		void Publish(long sequence);

		BatchDescriptor Next(int size);
		void Publish(BatchDescriptor batch);
	}

	public sealed class RingBufferBase<T> : IRingBuffer<T> where T : class
	{
		public T this[long sequence]
		{
			get { return this.inner[sequence]; }
		}

		public long Next()
		{
			return this.inner.Next();
		}
		public void Publish(long sequence)
		{
			this.inner.Publish(sequence);
		}

		public BatchDescriptor Next(int size)
		{
			return this.inner.Next(this.inner.NewBatchDescriptor(size));
		}
		public void Publish(BatchDescriptor batch)
		{
			this.inner.Publish(batch);
		}

		public RingBufferBase(RingBuffer<T> inner)
		{
			this.inner = inner;
		}

		private readonly RingBuffer<T> inner;
	}
}