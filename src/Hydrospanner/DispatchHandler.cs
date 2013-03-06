namespace Hydrospanner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Globalization;
	using System.Threading;
	using Disruptor;
	using RabbitMQ.Client;

	public class DispatchHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.WireId != Guid.Empty)
				return; // don't send anything that came off the wire

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
		private void Publish()
		{
			foreach (var message in this.buffer)
			{
				var properties = this.channel.CreateBasicProperties();
				properties.Headers = new Hashtable();
				CopyHeaders(message.Headers, properties.Headers);

				// TODO: 16-bytes: 4-byte node ID + 8-byte message sequence...
				properties.MessageId = message.MessageSequence.ToString(CultureInfo.InvariantCulture); // deterministic

				var exchange = message.Body.GetType().FullName.ToLower().Replace(".", "-"); // NanoMessageBus convention
				this.channel.BasicPublish(exchange, string.Empty, false, false, properties, message.SerializedBody);
			}

			this.channel.TxCommit(); // circuit breaker pattern
		}
		private static void CopyHeaders(Dictionary<string, string> source, IDictionary target)
		{
			foreach (var item in source)
				target[item.Key] = item.Value;
		}

		private static readonly TimeSpan DelayBeforeReconnect = TimeSpan.FromSeconds(1);
		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private IConnection connection;
		private IModel channel;
	}
}