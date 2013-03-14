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
			BasicDeliverEventArgs delivery;
			return this.subscription.Next((int)timeout.TotalMilliseconds, out delivery) ? delivery : null;
		}

		public RabbitSubscription(IModel channel, string queue)
		{
			this.subscription = new Subscription(channel, queue, AcknowledgeAllMessages);
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

		private const bool AcknowledgeAllMessages = false; // false here means to ack everything
		private readonly Subscription subscription;
		private bool disposed;
	}
}