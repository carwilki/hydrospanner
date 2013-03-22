namespace Hydrospanner.Configuration
{
	using System;
	using Messaging;
	using Messaging.Rabbit;

	public class MessagingFactory
	{
		public virtual IMessageReceiver CreateMessageReceiver()
		{
			return new RabbitChannel(this.connector, this.nodeId, x => new RabbitSubscription(x, this.sourceQueue));
		}
		public virtual IMessageSender CreateMessageSender()
		{
			return new RabbitChannel(this.connector, this.nodeId);
		}

		public MessagingFactory(short nodeId, Uri messageBroker, string sourceQueue)
		{
			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			if (messageBroker == null)
				throw new ArgumentNullException("messageBroker");

			if (string.IsNullOrWhiteSpace(sourceQueue))
				throw new ArgumentNullException("sourceQueue");

			this.nodeId = nodeId;
			this.sourceQueue = sourceQueue;
			this.connector = new RabbitConnector(messageBroker);
		}

		protected MessagingFactory()
		{
		}

		private readonly short nodeId;
		private readonly string sourceQueue;
		private readonly RabbitConnector connector;
	}
}