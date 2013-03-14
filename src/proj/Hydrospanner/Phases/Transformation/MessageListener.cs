namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Threading;
	using Disruptor;
	using Hydrospanner.Messaging;

	public class MessageListener : IDisposable
	{
		public void Start()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(MessageListener).Name);

			if (this.started)
				return;

			this.started = true;
			new Thread(this.StartListening).Start();
		}
		private void StartListening()
		{
			while (this.started)
				this.Publish(this.receiver.Receive(DefaultTimeout));
		}
		private void Publish(MessageDelivery delivery)
		{
			if (!delivery.Populated)
				return;

			var claimed = this.ring.Next();

			var item = this.ring[claimed];
			item.AsForeignMessage(
				delivery.Payload,
				delivery.MessageType,
				delivery.Headers,
				delivery.MessageId,
				delivery.Acknowledge);

			this.ring.Publish(claimed);
		}

		public MessageListener(IMessageReceiver receiver, RingBuffer<TransformationItem> ring)
		{
			if (receiver == null)
				throw new ArgumentNullException("receiver");

			if (ring == null)
				throw new ArgumentNullException("ring");

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
			if (!disposing || this.disposed)
				return;

			this.started = false;
			this.disposed = true;
			this.receiver.Dispose();
		}

		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
		private readonly IMessageReceiver receiver;
		private readonly RingBuffer<TransformationItem> ring;
		private bool started;
		private bool disposed;
	}
}