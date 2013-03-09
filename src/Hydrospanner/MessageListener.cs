namespace Hydrospanner
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Text;
	using System.Threading;
	using Disruptor;
	using RabbitMQ.Client;
	using RabbitMQ.Client.Events;
	using RabbitMQ.Client.MessagePatterns;

	public class MessageListener : IDisposable
	{
		public void Start()
		{
			if (this.started)
				return;

			this.started = true;

			new Thread(() =>
			{
				while (this.started)
					this.TryConsume();
			}).Start();
		}
		public void Stop()
		{
			this.started = false;
			this.Disconnect();
		}

		private void TryConsume()
		{
			if (!this.Connect())
				return;

			try
			{
				this.Publish(this.Consume());
			}
			catch
			{
				this.Disconnect();
			}
		}
		private BasicDeliverEventArgs Consume()
		{
			var current = this.subscription;
			if (current == null)
				return null;

			if (!this.started)
				return null;

			BasicDeliverEventArgs args;
			if (!current.Next(AwaitMessageTimeout, out args))
				return null;

			if (args == null)
				return null;

			if (!this.started)
				return null;

			if (args.Body == null || args.Body.Length == 0)
				return null;

			return args;
		}
		private bool Connect()
		{
			var local = this.channel;
			if (local != null && local.CloseReason != null)
				this.Disconnect();

			while (this.started && this.channel == null)
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

			return this.channel != null;
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
			this.channel.BasicQos(0, ushort.MaxValue, false);
			this.subscription = new Subscription(this.channel, QueueName, false);
		}

		private void Disconnect()
		{
			if (this.channel != null)
				this.channel.TryDispose();

			if (this.connection != null)
				this.connection.TryDispose();

			this.subscription = null;
			this.channel = null;
			this.connection = null;
		}
		private void Publish(BasicDeliverEventArgs delivery)
		{
			if (delivery == null)
				return;

			var properties = delivery.BasicProperties;
			if (properties == null)
				return;
			
			var claimed = this.ring.Next();
			var message = this.ring[claimed];
			message.Clear();

			message.SerializedBody = delivery.Body;
			message.Headers = ParseHeaders(properties.Headers);
			message.WireId = GetMessageId(properties.MessageId);
			var tag = delivery.DeliveryTag;

			message.AcknowledgeDelivery = () =>
			{
				try
				{
					Console.WriteLine("Acknowledged message {0}", tag);
					this.channel.BasicAck(tag, true);
				}
// ReSharper disable EmptyGeneralCatchClause
				catch
// ReSharper restore EmptyGeneralCatchClause
				{
				}
			};

			this.ring.Publish(claimed);
		}

		private static Dictionary<string, string> ParseHeaders(IDictionary source)
		{
			var target = new Dictionary<string, string>(source.Count);
			foreach (var key in source.Keys)
				target[key as string ?? string.Empty] = Encoding.UTF8.GetString(source[key] as byte[] ?? new byte[0]);

			return target;
		}
		private static Guid GetMessageId(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return Guid.Empty;

			Guid id;
			if (Guid.TryParse(raw, out id))
				return id;

			long numeric;
			if (!long.TryParse(raw, out numeric))
				return Guid.Empty;

			BitConverter.GetBytes(numeric).CopyTo(MessageIdBytes, 0);
			return new Guid(MessageIdBytes);
		}

		public MessageListener(RingBuffer<WireMessage> ring)
		{
			this.ring = ring;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (disposing && this.started)
				this.Stop();
		}

		private static readonly byte[] MessageIdBytes = new byte[16];
		private static readonly TimeSpan DelayBeforeReconnect = TimeSpan.FromSeconds(1);
		private static readonly int AwaitMessageTimeout = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private static readonly string QueueName = ConfigurationManager.AppSettings["queue-name"];
		private readonly RingBuffer<WireMessage> ring;
		private Subscription subscription;
		private IConnection connection;
		private IModel channel;
		private bool started;
	}
}