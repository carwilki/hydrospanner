namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Globalization;
	using Phases.Journal;
	using RabbitMQ.Client;

	internal class RabbitChannel : IMessageSender, IMessageReceiver
	{
		// NOTE: caller should be responsible for while loop where all actions return success.
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

			var currentChannel = this.channel = this.channel ?? this.connector.OpenChannel();
			if (currentChannel == null)
				return false;

			// FUTURE: Any correlation ID could potentially be stored in the message headers and then extracted.
			// Also, on the receiving side we could do the same thing in reverse.
			// FUTURE: TTL and DeliveryMode could be in an application-defined dictionary that is available for lookup here
			// based upon message type.  Default to Persistent, no TTL if an entry is not found.
			var meta = currentChannel.CreateBasicProperties();
			meta.AppId = this.appId;
			meta.DeliveryMode = Persistent;
			meta.Type = message.SerializedType;
			meta.Timestamp = new AmqpTimestamp(SystemTime.EpochUtcNow);
			meta.MessageId = message.MessageSequence.ToMessageId(this.nodeId);
			meta.ContentType = ContentType; // TODO: +json, +pb, +msgpack, +kryo, etc.
			meta.ContentEncoding = meta.ContentEncoding; // TODO
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

			var instance = this.channel;
			if (instance == null)
				return false;

			try
			{
				instance.TxCommit();
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

			var currentChannel = this.channel = this.channel ?? this.connector.OpenChannel();
			if (currentChannel == null)
				return MessageDelivery.Empty;

			currentChannel.BasicQos(0, ushort.MaxValue, false);
			var currentSubscription = this.subscription = this.subscription ?? this.factory(currentChannel);

			if (!currentChannel.IsOpen)
			{
				this.Close();
				return MessageDelivery.Empty;
			}

			var message = currentSubscription.Receive(timeout);
			if (message == null)
				return MessageDelivery.Empty;

			var meta = message.BasicProperties;
			if (meta.AppId == this.appId)
				return MessageDelivery.Empty; // the message originated at this node, don't re-consume it

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
					return;
				}
			});
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
			this.appId = nodeId.ToString(CultureInfo.InvariantCulture);
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
		private const string ContentType = "application/vnd.nmb.hydrospanner-msg";
		private readonly RabbitConnector connector;
		private readonly Func<IModel, RabbitSubscription> factory;
		private readonly short nodeId;
		private readonly string appId;
		private IModel channel;
		private RabbitSubscription subscription;
		private bool disposed;
	}
}