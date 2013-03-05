namespace Hydrospanner.Outbox
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Threading;
	using Disruptor;
	using Hydrospanner.Inbox;
	using RabbitMQ.Client;

	public class DispatchHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			this.buffer.Add(data);

			if (!endOfBatch)
				return;

			this.TryPublish();
			this.buffer.Clear();
		}
		private void TryPublish()
		{
			this.Connect();

			try
			{
				this.Publish();
			}
			catch
			{
				this.Disconnect();
			}
		}
		private void Publish()
		{
			for (var i = 0; i < this.buffer.Count; i++)
			{
				var properties = this.channel.CreateBasicProperties();

				// TODO: append headers
				properties.MessageId = null; // TODO: unique AND deterministic sequence number (CRITICAL for de-duplication)
				var message = this.buffer[i];
				var exchange = message.Body.GetType().FullName.ToLower().Replace(".", "-"); // NanoMessageBus convention
				this.channel.BasicPublish(exchange, null, false, false, properties, message.Payload);
			}

			this.channel.TxCommit(); // circuit breaker pattern
		}
		
		private void Connect()
		{
			var local = this.channel;
			if (local != null && local.CloseReason != null)
				this.Disconnect();

			while (this.channel == null)
			{
				try
				{
					this.TryConnect();
				}
				catch
				{
					this.Disconnect();
					Thread.Sleep(DelayBeforeReconnect);
				}
			}
		}
		private void TryConnect()
		{
			var authentication = ServerAddress.UserInfo.Split(new[] { ':' });
			var factory = new ConnectionFactory
			{
				Endpoint = new AmqpTcpEndpoint(ServerAddress),
				UserName = authentication.Length > 0 ? authentication[0] : null,
				Password = authentication.Length > 1 ? authentication[1] : null
			};

			this.connection = factory.CreateConnection();
			this.channel = this.connection.CreateModel();
			this.channel.TxSelect();
		}

		private void Disconnect()
		{
			if (this.channel != null)
				this.channel.TryDispose();

			if (this.connection != null)
				this.connection.TryDispose();

			this.channel = null;
			this.connection = null;
		}

		private static readonly TimeSpan DelayBeforeReconnect = TimeSpan.FromSeconds(1);
		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private readonly List<DispatchMessage> buffer = new List<DispatchMessage>();
		private IConnection connection;
		private IModel channel;
	}
}