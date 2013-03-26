namespace Hydrospanner
{
	using Disruptor;

	public interface IRingBuffer<T> where T : class
	{
		long Next();
		T this[long sequence] { get; }
		void Publish(long sequence);

		BatchDescriptor NewBatchDescriptor(int size);
		void Publish(BatchDescriptor batch);
	}

	public sealed class RingBufferBase<T> : IRingBuffer<T> where T : class
	{
		public long Next()
		{
			return this.inner.Next();
		}

		public T this[long sequence]
		{
			get { return this.inner[sequence]; }
		}

		public void Publish(long sequence)
		{
			this.inner.Publish(sequence);
		}

		public BatchDescriptor NewBatchDescriptor(int size)
		{
			return this.inner.NewBatchDescriptor(size);
		}

		public void Publish(BatchDescriptor batch)
		{
			this.inner.Publish(batch);
		}

		public RingBufferBase(RingBuffer<T> inner)
		{
			this.inner = inner;
		}

		readonly RingBuffer<T> inner;
	}
}