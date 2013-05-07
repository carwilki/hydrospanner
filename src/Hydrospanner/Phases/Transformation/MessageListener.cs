namespace Hydrospanner.Phases.Transformation
{
	using System;
	using System.Collections.Generic;
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

			// if a received message fails deserialization, is rejected as poison, and is republished to this queue
			// without fixing the code and restarting the process, it will be discarded as a duplicate message.
			if (this.duplicates.Contains(delivery.MessageId))
			{
				// we get a deadlock because no new messages can be received, but we also don't want to ack things we shouldn't...
				// perhaps we count # of dups in a row and if it reaches duplicate capacity, we ack?
				delivery.Acknowledge(true); // TODO: get this under test; also, do we really want to ack all messages up to this point when a duplicate is received?
				Log.DebugFormat("Rejecting message {0} of type '{1}' as duplicate.", delivery.MessageId, delivery.MessageType);
			}
			else if (this.transients.Contains(delivery.MessageType ?? string.Empty))
				this.PublishTransientMessageToRingBuffer(delivery);
			else
				this.PublishJournaledMessageToRingBuffer(delivery);
		}
		private void PublishTransientMessageToRingBuffer(MessageDelivery delivery)
		{
			Log.DebugFormat("New transient message {0} of type '{1}' arrived, pushing to disruptor.", delivery.MessageId, delivery.MessageType);
			var next = this.ring.Next();
			var claimed = this.ring[next];
			claimed.AsTransientMessage(
				delivery.Payload,
				delivery.MessageType,
				delivery.Headers,
				delivery.MessageId,
				delivery.Acknowledge);
			this.ring.Publish(next);
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

		public MessageListener(Func<IMessageReceiver> receiverFactory, IRingBuffer<TransformationItem> ring, DuplicateStore duplicates, ICollection<string> transients)
		{
			if (receiverFactory == null)
				throw new ArgumentNullException("receiverFactory");

			if (ring == null)
				throw new ArgumentNullException("ring");

			if (duplicates == null)
				throw new ArgumentNullException("duplicates");

			if (transients == null)
				throw new ArgumentNullException("transients");

			this.receiverFactory = receiverFactory;
			this.ring = ring;
			this.duplicates = duplicates;
			this.transients = transients;
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
		private readonly ICollection<string> transients;
		private bool started;
		private bool disposed;
	}
}