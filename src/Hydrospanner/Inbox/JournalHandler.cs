namespace Hydrospanner.Inbox
{
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Text;
	using Disruptor;

	public class JournalHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			// TODO: where does de-duplication happen?
			data.StreamId = this.identifier.DiscoverStreams(data.Body, data.Headers);
			data.IncomingSequence = ++this.storedSequence;

			this.buffer.Add(data);
			if (endOfBatch || this.buffer.Count > 418)
				this.JournalMessages();
		}
		private void JournalMessages()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			using (var command = connection.CreateCommand())
			{
				// TODO: optimize with less writes to database and optimize loop to operate within the same transaction
				// (aggregate unique values such as stream id or wire id when null.  Sequence can be derived from the first or *database-provided*)
				// we don't technically need 

				var builder = new StringBuilder(this.buffer.Count * InsertStatement.Length);

				for (var i = 0; i < this.buffer.Count; i++)
				{
					command.WithParameter("@seq{0}".FormatWith(i), this.buffer[i].IncomingSequence);
					command.WithParameter("@stream{0}".FormatWith(i), this.buffer[i].StreamId);
					command.WithParameter("@wire{0}".FormatWith(i), this.buffer[i].WireId);
					command.WithParameter("@payload{0}".FormatWith(i), this.buffer[i].Payload);
					command.WithParameter("@headers{0}".FormatWith(i), new byte[0]);
					builder.AppendFormat(InsertStatement, i);
				}

				command.Transaction = transaction;
				command.CommandText = builder.ToString();

				command.ExecuteNonQuery(); // TODO: circuit breaker pattern
				transaction.Commit();

				this.buffer.Clear();
			}
		}

		public JournalHandler(string connectionName, IStreamIdentifier<object> identifier)
		{
			this.identifier = identifier;

			this.settings = ConfigurationManager.ConnectionStrings[connectionName];

			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT MAX(sequence) FROM [messages];";
				var value = command.ExecuteScalar();
				if (value is long)
					this.storedSequence = (long)value;
			}
		}

		private const string InsertStatement = "INSERT INTO [messages] VALUES ( @seq{0}, @stream{0}, @wire{0}, @payload{0}, @headers{0} );\n";
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly ConnectionStringSettings settings;
		private readonly IStreamIdentifier<object> identifier;
		private long storedSequence;
	}
}