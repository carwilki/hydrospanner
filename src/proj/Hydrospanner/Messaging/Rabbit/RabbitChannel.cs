namespace Hydrospanner.Messaging.Rabbit
{
	using System;
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

			var instance = this.channel;
			if (instance == null)
				this.channel = instance = this.connector.OpenChannel();

			if (instance == null)
				return false;

			var properties = instance.CreateBasicProperties();

			// TODO: message id, delivery mode, copy headers, expiration, timestamp, app id (this node id?)
			// TODO: content encoding, content-type, correlation id?
			// TODO: what happens if the serialized body is null and/or the serialized type is null/empty?
			var exchange = message.SerializedType.ToLowerInvariant().Replace(".", "-");

			try
			{
				instance.BasicPublish(exchange, string.Empty, properties, message.SerializedBody);
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

		public RabbitChannel(RabbitConnector connector)
		{
			if (connector == null)
				throw new ArgumentNullException();

			this.connector = connector;
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

		private readonly RabbitConnector connector;
		private IModel channel;
		private bool disposed;
	}
}