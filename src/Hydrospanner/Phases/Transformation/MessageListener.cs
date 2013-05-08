namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Threading;
	using log4net;
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

			// NOTE: if a received message fails deserialization, is rejected as poison, and is republished to this queue
			// without fixing the code and restarting the process, it will be discarded as a duplicate message.
			if (this.duplicates.Contains(delivery.MessageId))
			{
				if (delivery.Acknowledge != null)
					delivery.Acknowledge(Acknowledgment.ConfirmSingle);

				Log.DebugFormat("Rejecting message {0} of type '{1}' as duplicate.", delivery.MessageId, delivery.MessageType);
			}
			else
				this.PublishJournaledMessageToRingBuffer(delivery);
		}
		private void PublishJournaledMessageToRingBuffer(MessageDelivery delivery)
		{
			Log.DebugFormat("New journaled message {0} of type '{1}' arrived, pushing to disruptor.", delivery.MessageId, delivery.MessageType);
			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsForeignMessage(
				delivery.Payload,
				delivery.MessageType,
				delivery.Headers,
				delivery.MessageId,
				delivery.Acknowledge);
			this.ring.Publish(next);
		}

		public MessageListener(Func<IMessageReceiver> receiverFactory, IRingBuffer<TransformationItem> ring, DuplicateStore duplicates)
		{
			if (receiverFactory == null)
				throw new ArgumentNullException("receiverFactory");

			if (ring == null)
				throw new ArgumentNullException("ring");

			if (duplicates == null)
				throw new ArgumentNullException("duplicates");

			this.receiverFactory = receiverFactory;
			this.ring = ring;
			this.duplicates = duplicates;
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

		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageListener));
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
		private readonly Func<IMessageReceiver> receiverFactory;
		private readonly IRingBuffer<TransformationItem> ring;
		private readonly DuplicateStore duplicates;
		private bool started;
		private bool disposed;
	}
}