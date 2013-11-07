namespace Hydrospanner.Wireup
{
	using System;

	using Hydrospanner.Messaging.Azure;

	using Messaging;
	using Messaging.Rabbit;
	using Phases.Transformation;
	using RabbitMQ.Client;

    public class AzureMessagingFactory : MessagingFactory
    {
        private readonly DuplicateStore duplicates;

        public AzureMessagingFactory(string sourceQueue, DuplicateStore duplicates)
        {
            this.duplicates = duplicates;
        }

        public override MessageListener CreateMessageListener(IRingBuffer<TransformationItem> ring)
        {
            return new MessageListener(this.NewReceiver, ring, duplicates);
        }

        public override IMessageSender CreateNewMessageSender()
        {
            return new AzureServiceBusChannel();
        }

        private IMessageReceiver NewReceiver()
        {
            return new AzureServiceBusChannel();
        }
    }

	public class MessagingFactory
	{
		public virtual IMessageSender CreateNewMessageSender()
		{
			return new RabbitChannel(this.connector, this.nodeId);
		}
		public virtual MessageListener CreateMessageListener(IRingBuffer<TransformationItem> ring)
		{
			return new MessageListener(this.NewReceiver, ring, this.duplicates);
		}
		private IMessageReceiver NewReceiver()
		{
			return new RabbitChannel(this.connector, this.nodeId, this.NewSubscription);
		}
		private RabbitSubscription NewSubscription(IModel channel)
		{
			return new RabbitSubscription(channel, this.sourceQueue);
		}

		public MessagingFactory(short nodeId, Uri messageBroker, string sourceQueue, DuplicateStore duplicates) : this()
		{
			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			if (messageBroker == null)
				throw new ArgumentNullException("messageBroker");

			if (string.IsNullOrWhiteSpace(sourceQueue))
				throw new ArgumentNullException("sourceQueue");

			if (duplicates == null)
				throw new ArgumentNullException("duplicates");

			this.nodeId = nodeId;
			this.sourceQueue = sourceQueue;
			this.duplicates = duplicates;
			this.connector = new RabbitConnector(messageBroker);
		}
		protected MessagingFactory()
		{
		}

		private readonly short nodeId;
		private readonly string sourceQueue;
		private readonly DuplicateStore duplicates;
		private readonly RabbitConnector connector;
	}
}
