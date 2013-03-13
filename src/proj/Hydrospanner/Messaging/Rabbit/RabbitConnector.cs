namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using RabbitMQ.Client;

	public class RabbitConnector : IDisposable
	{
		public virtual IModel OpenChannel()
		{
			return null;
		}

		public RabbitConnector(Uri address) : this(address, new ConnectionFactory())
		{
		}
		public RabbitConnector(Uri address, ConnectionFactory factory)
		{
			this.address = address;
			this.factory = factory;
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;
		}

		private readonly Uri address;
		private readonly ConnectionFactory factory;
	}
}