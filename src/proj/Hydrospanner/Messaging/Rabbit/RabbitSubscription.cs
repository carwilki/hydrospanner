namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.MessagePatterns;

	internal class RabbitSubscription : IDisposable
	{
		public virtual BasicDeliverEventArgs Receive(TimeSpan timeout)
		{
			try
			{
				BasicDeliverEventArgs delivery;
				return this.subscription.Next((int)timeout.TotalMilliseconds, out delivery) ? delivery : null;
			}
			catch
			{
				this.channel.TryDispose();
				return null;
			}
		}

		public RabbitSubscription(IModel channel, string queue)
		{
			if (channel == null)
				throw new ArgumentNullException("channel");

			if (string.IsNullOrWhiteSpace(queue))
				throw new ArgumentNullException("queue");

			this.channel = channel;
			this.subscription = new Subscription(channel, queue, AcknowledgeAllMessages);
		}
		protected RabbitSubscription()
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

			this.disposed = true;
			this.subscription.Close();
		}

		private const bool AcknowledgeAllMessages = false; // false here = message require ack
		private readonly Subscription subscription;
		private readonly IModel channel;
		private bool disposed;
	}
}