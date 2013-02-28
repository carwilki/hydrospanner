namespace Hydrospanner
{
	using System;
	using System.Collections;
	using System.Configuration;
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
			this.Connect();

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

			var claimed = this.ring.Next();
			var message = this.ring[claimed];
			message.Clear();
			message.Payload = delivery.Body;
			message.Headers = (Hashtable)delivery.BasicProperties.Headers;
			var tag = delivery.DeliveryTag;

			message.ConfirmDelivery = () =>
			{
				try
				{
					Console.WriteLine("Acknowledged message {0}", tag);
					channel.BasicAck(tag, true);
				}
// ReSharper disable EmptyGeneralCatchClause
				catch
// ReSharper restore EmptyGeneralCatchClause
				{
				}
			};

			this.ring.Publish(claimed);
		}

		public MessageListener(RingBuffer<ReceivedMessage> ring)
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

		private static readonly TimeSpan DelayBeforeReconnect = TimeSpan.FromSeconds(1);
		private static readonly int AwaitMessageTimeout = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
		private static readonly Uri ServerAddress = new Uri(ConfigurationManager.AppSettings["rabbit-server"]);
		private static readonly string QueueName = ConfigurationManager.AppSettings["queue-name"];
		private readonly RingBuffer<ReceivedMessage> ring;
		private Subscription subscription;
		private IConnection connection;
		private IModel channel;
		private bool started;
	}
}