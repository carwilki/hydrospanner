namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using RabbitMQ.Client;

	public class RabbitConnector : IDisposable
	{
		public virtual IModel OpenChannel()
		{
			if (this.disposed)
				throw new ObjectDisposedException(typeof(RabbitConnector).Name);

			return this.TryOpenChannel();
		}
		private IModel TryOpenChannel()
		{
			try
			{
				var currentConnection = this.Connect();
				return currentConnection == null ? null : currentConnection.CreateModel();
			}
			catch
			{
				this.Disconnect();
				ConnectionFailureTimeout.Sleep();
				return null;
			}
		}
		private IConnection Connect()
		{
			lock (this.sync)
			{
				var currentConnection = this.connection;
				if (currentConnection == null)
					this.connection = currentConnection = this.factory.CreateConnection();

				return currentConnection;
			}
		}
		private void Disconnect()
		{
			lock (this.sync)
			{
				var currentConnection = this.connection;
				this.connection = currentConnection.TryDispose();
			}
		}

		public RabbitConnector(Uri address, ConnectionFactory factory = null) : this()
		{
			if (address == null)
				throw new ArgumentNullException("address");

			this.factory = factory ?? RabbitConnectionParser.Parse(address);
		}
		protected RabbitConnector()
		{
			this.sync = new object();
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
			this.Disconnect();
		}

		private static readonly TimeSpan ConnectionFailureTimeout = TimeSpan.FromSeconds(3);
		private readonly ConnectionFactory factory;
		private readonly object sync;
		private IConnection connection;
		private bool disposed;
	}
}