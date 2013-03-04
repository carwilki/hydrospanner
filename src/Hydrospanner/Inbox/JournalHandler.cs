namespace Hydrospanner.Inbox
{
	using System.Collections.Generic;
	using System.Configuration;
	using Disruptor;

	public class JournalHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// TODO: where does de-duplication happen?

			data.IncomingSequence = ++this.storedSequence;
			// TODO: callback into application code to determine the stream of a given message

			this.buffer.Add(data);
			if (endOfBatch)
				this.JournalMessages();
		}
		private void JournalMessages()
		{
			// TODO: append sequence number to each journaled message
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = string.Empty;
				command.ExecuteNonQuery(); // TODO: circuit breaker pattern
			}
		}

		public JournalHandler(string connectionName)
		{
			// TODO: get max sequence number
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
		}

		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly ConnectionStringSettings settings;
		private long storedSequence;
	}
}