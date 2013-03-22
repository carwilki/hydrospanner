namespace Hydrospanner.Phases
{
	using System;
	using Disruptor;

	public interface IDisruptor<T> : IDisposable where T : class
	{
		RingBuffer<T> RingBuffer { get; }
		void Start();
		void Stop();
	}
}