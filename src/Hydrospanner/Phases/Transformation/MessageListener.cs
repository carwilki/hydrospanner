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
			{
				try
				{
					Log.Debug("Now listening for messages.");
					while (this.started)
						this.Publish(receiver.Receive(DefaultTimeout));
				}
				catch (Exception)
				{
					Log.Debug("Failure listening to messages occurred.");
					throw; // TODO: get under test
				}
			}
		}
		private void Publish(MessageDelivery delivery)
		{
			if (!delivery.Populated)
				return;

			Log.DebugFormat(
				"New message ({0}) of type '{1}' arrived, publishing to transformation disruptor.",
				delivery.MessageId, 
				delivery.MessageType);

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

		public MessageListener(Func<IMessageReceiver> receiverFactory, IRingBuffer<TransformationItem> ring)
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

		private static readonly ILog Log = LogManager.GetLogger(typeof(MessageListener));
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
		private readonly Func<IMessageReceiver> receiverFactory;
		private readonly IRingBuffer<TransformationItem> ring;
		private bool started;
		private bool disposed;
	}
}