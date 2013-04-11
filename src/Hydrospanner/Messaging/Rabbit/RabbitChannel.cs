namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Globalization;
	using Phases.Journal;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;

	public class RabbitChannel : IMessageSender, IMessageReceiver
	{
		public bool Send(JournalItem message)
		{
			if (message == null)
				throw new ArgumentNullException();

			if (this.disposed)
				return false;

			if (!message.ItemActions.HasFlag(JournalItemAction.Dispatch))
				return true; // no op

			if (message.SerializedBody == null || message.SerializedBody.Length == 0)
				return true; // no op

			if (string.IsNullOrWhiteSpace(message.SerializedType))
				return true;

			var currentChannel = this.OpenChannel(false);
			if (currentChannel == null)
				return false;

			// FUTURE: Any correlation ID could potentially be stored in the message headers and then extracted.
			// Also, on the receiving side we could do the same thing in reverse.

			// FUTURE: TTL and DeliveryMode could be in an application-defined dictionary that is available for lookup here
			// based upon message type.  Default to Persistent, no TTL if an entry is not found.

			// FUTURE: ContentType and ContentEncoding will be dynamic based upon serialization, e.g. +json, +msgpack, +pb, etc.
			var meta = currentChannel.CreateBasicProperties();
			meta.AppId = this.normalizedNodeId;
			meta.DeliveryMode = Persistent;
			meta.Type = message.SerializedType;
			meta.Timestamp = new AmqpTimestamp(SystemTime.EpochUtcNow);
			meta.MessageId = message.MessageSequence.ToMessageId(this.nodeId);
			meta.ContentType = ContentType;
			meta.ContentEncoding = ContentEncoding;
			meta.Headers = message.Headers.CopyTo(meta.Headers);
			var exchange = message.SerializedType.NormalizeType();

			try
			{
				currentChannel.BasicPublish(exchange, string.Empty, meta, message.SerializedBody);
			}
			catch
			{
				this.Close();
				return false;
			}

			return true;
		}
		public bool Commit()
		{
			if (this.disposed)
				return false;

			var currentChannel = this.channel;
			if (currentChannel == null)
				return false;

			try
			{
				currentChannel.TxCommit();
				return true;
			}
			catch
			{
				this.Close();
				return false;
			}
		}

		public MessageDelivery Receive(TimeSpan timeout)
		{
			if (this.disposed)
				return MessageDelivery.Empty;

			var currentChannel = this.OpenChannel(true);
			if (currentChannel == null)
				return MessageDelivery.Empty;

			var currentSubscription = this.OpenSubscription(currentChannel);
			if (currentSubscription == null)
				return MessageDelivery.Empty;

			if (currentChannel.IsOpen)
				return this.ReceiveMessage(currentChannel, currentSubscription.Receive(timeout));

			this.Close();
			return MessageDelivery.Empty;
		}
		private MessageDelivery ReceiveMessage(IModel currentChannel, BasicDeliverEventArgs message)
		{
			if (message == null)
				return MessageDelivery.Empty;

			var meta = message.BasicProperties;
			if (meta.AppId == this.normalizedNodeId)
				return MessageDelivery.Empty; // the message originated at this node, don't re-consume it; FUTURE: how does this affect timeout messages?

			var id = meta.MessageId.ToMessageId();
			var headers = meta.Headers.Copy();
			var tag = message.DeliveryTag;

			return new MessageDelivery(id, message.Body, meta.Type, headers, () =>
			{
				if (this.disposed || !currentChannel.IsOpen)
					return;

				try
				{
					currentChannel.BasicAck(tag, true);
				}
				catch
				{
// ReSharper disable RedundantJumpStatement
					return;
// ReSharper restore RedundantJumpStatement
				}
			});
		}

		private IModel OpenChannel(bool receive)
		{
			var currentChannel = this.channel;
			if (currentChannel != null)
				return currentChannel;

			this.channel = currentChannel = this.connector.OpenChannel();
			if (currentChannel == null)
				return null;

			try
			{
				if (receive)
					currentChannel.BasicQos(0, ushort.MaxValue, false);
				else
					currentChannel.TxSelect();
			}
			catch
			{
				this.Close();
				return null;
			}

			return currentChannel;
		}
		private RabbitSubscription OpenSubscription(IModel currentChannel)
		{
			var currentSubscription = this.subscription;
			if (currentSubscription != null)
				return currentSubscription;

			try
			{
				currentSubscription = this.factory(currentChannel);
				this.subscription = currentSubscription;
				return currentSubscription;
			}
			catch
			{
				this.Close();
				return null;
			}
		}
		private void Close()
		{
			this.subscription = this.subscription.TryDispose();
			this.channel = this.channel.TryDispose();
		}

		public RabbitChannel(RabbitConnector connector, short nodeId, Func<IModel, RabbitSubscription> factory = null)
		{
			if (connector == null)
				throw new ArgumentNullException();

			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			this.connector = connector;
			this.nodeId = nodeId;
			this.factory = factory;
			this.normalizedNodeId = nodeId.ToString(CultureInfo.InvariantCulture);
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

			this.Close();
			this.connector.TryDispose();
		}

		private const byte Persistent = 2;
		private const string ContentType = "application/vnd.hydrospanner-msg+json";
		private const string ContentEncoding = "utf8";
		private readonly RabbitConnector connector;
		private readonly Func<IModel, RabbitSubscription> factory;
		private readonly short nodeId;
		private readonly string normalizedNodeId;
		private IModel channel;
		private RabbitSubscription subscription;
		private bool disposed;
	}
}