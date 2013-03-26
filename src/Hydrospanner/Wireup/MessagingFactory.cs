namespace Hydrospanner.Wireup
{
	using System;
	using Disruptor;
	using Messaging;
	using Messaging.Rabbit;
	using Phases.Transformation;

	public class MessagingFactory
	{
		public virtual MessageListener CreateMessageListener(RingBuffer<TransformationItem> ring)
		{
			return new MessageListener(
				() => new RabbitChannel(this.connector, this.nodeId, x => new RabbitSubscription(x, this.sourceQueue)),
				ring);
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