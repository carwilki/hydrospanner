namespace Hydrospanner.Phases.Transformation
{
	using System;
	using Disruptor;
	using Hydrospanner.Messaging;

	public class MessageListener : IDisposable
	{
		public void Start()
		{
		}
		private void Receive()
		{
		}

		public MessageListener(IMessageReceiver receiver, RingBuffer<object> ring)
		{
			this.receiver = receiver;
			this.ring = ring;
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
		}

		private readonly IMessageReceiver receiver;
		private readonly RingBuffer<object> ring;
		private bool started;
	}
}