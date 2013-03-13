namespace Hydrospanner.Messaging.Rabbit
{
	using System;
	using Hydrospanner.Phases.Journal;
	using RabbitMQ.Client;

	public class RabbitChannel : IMessageSender, IMessageReceiver
	{
		public void Send(JournalItem message)
		{
			throw new NotImplementedException();
		}
		public void Commit()
		{
			throw new NotImplementedException();
		}
		public MessageDelivery Receive(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		public RabbitChannel(RabbitConnector connector)
		{
			this.connector = connector;
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

		private readonly RabbitConnector connector;
		private IModel channel;
	}
}