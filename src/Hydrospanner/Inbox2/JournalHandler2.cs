namespace Hydrospanner.Inbox2
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Text;
	using Disruptor;

	public class JournalHandler2 : IEventHandler<WireMessage2>
	{
		public void OnNext(WireMessage2 data, long sequence, bool endOfBatch)
		{
			data.DuplicateMessage = this.duplicates.Contains(data.WireId);
			if (data.DuplicateMessage)
				return;

			data.StreamId = this.identifier.DiscoverStreams(data.Body, data.Headers);
			data.MessageSequence = ++this.currentSequence;

			this.buffer.Add(data);
			this.bookmark = Math.Max(data.SourceSequence, this.bookmark); // only works on wire messages

			if (endOfBatch)
				this.JournalMessages();
		}
		private void JournalMessages()
		{
			using (var connection = this.settings.OpenConnection())
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			using (var command = connection.CreateCommand())
			{
				// BIG FAT TODO: we need a while loop inside of this transaction to read a slice of the buffer up to X items at once
				// TODO: aggregate parameters to reduce payload

				var builder = new StringBuilder(this.buffer.Count * AppendMessage.Length);

				for (var i = 0; i < this.buffer.Count; i++)
				{
					var item = this.buffer[i];

					command.WithParameter("@stream{0}".FormatWith(i), item.StreamId);
					command.WithParameter("@wire{0}".FormatWith(i), item.WireId);
					command.WithParameter("@payload{0}".FormatWith(i), item.SerializedBody);
					command.WithParameter("@headers{0}".FormatWith(i), item.SerializedHeaders);
					builder.AppendFormat(AppendMessage, i);
				}

				builder.AppendFormat(UpdateBookmark, this.bookmark);

				command.Transaction = transaction;
				command.CommandText = builder.ToString();

				command.ExecuteNonQuery(); // TODO: circuit breaker pattern
				transaction.Commit();

				this.buffer.Clear();
			}
		}

		public JournalHandler2(string connectionName, IStreamIdentifier identifier)
		{
			this.identifier = identifier;

			this.settings = ConfigurationManager.ConnectionStrings[connectionName];

			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT MAX(sequence) FROM [messages];";
				var value = command.ExecuteScalar();
				if (value is long)
					this.currentSequence = (long)value;
			}
		}

		private const string AppendMessage = "INSERT INTO [messages] VALUES ( @seq{0}, @stream{0}, @wire{0}, @payload{0}, @headers{0} );\n";
		private const string UpdateBookmark = "UPDATE bookmarks SET sequence = {0} WHERE sequence < {0};";
		private readonly DuplicateStore duplicates = new DuplicateStore(1024 * 64);
		private readonly List<WireMessage2> buffer = new List<WireMessage2>();
		private readonly ConnectionStringSettings settings;
		private readonly IStreamIdentifier identifier;
		private long currentSequence;
		private long bookmark;
	}
}