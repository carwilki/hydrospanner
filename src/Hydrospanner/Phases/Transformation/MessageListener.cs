namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Threading;
	using Disruptor;
	using Messaging;

	public class MessageListener : IDisposable
	{
		public virtual void Start()
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
			using (var receiver = this.receiverFactory())
				while (this.started)
					this.Publish(receiver.Receive(DefaultTimeout));
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

		public MessageListener(Func<IMessageReceiver> receiverFactory, RingBuffer<TransformationItem> ring)
		{
			if (receiverFactory == null)
				throw new ArgumentNullException("receiverFactory");

			if (ring == null)
				throw new ArgumentNullException("ring");

			this.receiverFactory = receiverFactory;
			this.ring = ring;
		}
		protected MessageListener()
		{
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
		}

		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
		private readonly Func<IMessageReceiver> receiverFactory;
		private readonly RingBuffer<TransformationItem> ring;
		private bool started;
		private bool disposed;
	}
}