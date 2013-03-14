namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using System.Collections;
	using System.Globalization;
	using Hydrospanner.Phases.Journal;
	using RabbitMQ.Client;

	public class RabbitChannel : IMessageSender, IMessageReceiver
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

			var instance = this.channel;
			if (instance == null)
				this.channel = instance = this.connector.OpenChannel();

			if (instance == null)
				return false;

			var meta = instance.CreateBasicProperties();
			meta.MessageId = ((message.MessageSequence << 16) + this.nodeId).ToString(CultureInfo.InvariantCulture);
			meta.AppId = this.appId;
			meta.Type = message.SerializedType;
			meta.Timestamp = new AmqpTimestamp(SystemTime.EpochUtcNow);
			meta.ContentType = ContentType;
			meta.DeliveryMode = Persistent;
			var headers = meta.Headers = meta.Headers ?? new Hashtable();
			foreach (var item in message.Headers)
				headers[item.Key] = item.Value;

			// FUTURE: Any correlation ID could potentially be stored in the message headers and then extracted.
			// Also, on the receiving side we could do the same thing in reverse.

			// FUTURE: TTL and DeliveryMode could be in an application-defined dictionary that is available for lookup here
			// based upon message type.  Default to Persistent, no TTL if an entry is not found.

			// TODO: Content Encoding
			var exchange = message.SerializedType.ToLowerInvariant().Replace(".", "-");

			try
			{
				instance.BasicPublish(exchange, string.Empty, meta, message.SerializedBody);
			}
			catch
			{
				this.CloseChannel();
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
				this.CloseChannel();
				return false;
			}
		}

		public MessageDelivery Receive(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		private void CloseChannel()
		{
			var instance = this.channel;
			if (instance != null)
				instance.TryDispose();

			this.channel = null;
		}

		public RabbitChannel(RabbitConnector connector, short nodeId)
		{
			if (connector == null)
				throw new ArgumentNullException();

			if (nodeId <= 0)
				throw new ArgumentOutOfRangeException("nodeId");

			this.connector = connector;
			this.nodeId = nodeId;
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

			this.CloseChannel();
			this.connector.TryDispose();
			this.channel = null;
		}

		private const byte Persistent = 2;
		private const string ContentType = "application/vnd.nmb.hydrospanner-msg";
		private readonly RabbitConnector connector;
		private readonly short nodeId;
		private readonly string appId;
		private IModel channel;
		private bool disposed;
	}
}