namespace Hydrospanner.Wireup
{
	using System;
	using System.Collections.Generic;
	using Messaging;
	using Messaging.Rabbit;
	using Phases.Transformation;
	using RabbitMQ.Client;

	public class MessagingFactory
	{
		public virtual IMessageSender CreateSnapshotMessageSender()
		{
			return new RabbitChannel(this.connector, this.nodeId);
		}
		public virtual IMessageSender CreateJournalMessageSender()
		{
			return new RabbitChannel(this.connector, this.nodeId);
		}
		public virtual MessageListener CreateMessageListener(IRingBuffer<TransformationItem> ring)
		{
			return new MessageListener(this.NewReceiver, ring, this.duplicates, this.transients);
		}
		private IMessageReceiver NewReceiver()
		{
			return new RabbitChannel(this.connector, this.nodeId, this.NewSubscription);
		}
		private RabbitSubscription NewSubscription(IModel channel)
		{
			return new RabbitSubscription(channel, this.sourceQueue);
		}

		public MessagingFactory(short nodeId, Uri messageBroker, string sourceQueue, DuplicateStore duplicates, ICollection<string> transients) : this()
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
			this.transients = transients;
		}
		protected MessagingFactory()
		{
		}

		private readonly short nodeId;
		private readonly string sourceQueue;
		private readonly DuplicateStore duplicates;
		private readonly RabbitConnector connector;
		private readonly ICollection<string> transients;
	}
}
