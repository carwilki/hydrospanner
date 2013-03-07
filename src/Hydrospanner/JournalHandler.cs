namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Text;
	using Disruptor;

	public class JournalHandler : IEventHandler<WireMessage>
	{
		public void OnNext(WireMessage data, long sequence, bool endOfBatch)
		{
			if (data.DuplicateMessage)
				return; // everything hereafter should ignore this message

			if (data.MessageSequence > 0)
				return; // this message has already been journaled

			data.MessageSequence = ++this.currentSequence;

			this.buffer.Add(data);

			// checkpoint the source message sequence that caused this message
			this.transformationCheckpoint = Math.Max(data.SourceSequence, this.transformationCheckpoint);

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

				builder.AppendFormat(UpdateTransformationCheckpoint, this.transformationCheckpoint);

				command.Transaction = transaction;
				command.CommandText = builder.ToString();

				command.ExecuteNonQuery();
				transaction.Commit();

				this.buffer.Clear();
			}
		}

		public JournalHandler(string connectionName, long maxSequence)
		{
			this.settings = ConfigurationManager.ConnectionStrings[connectionName];
			this.currentSequence = maxSequence;
		}

		private const string AppendMessage = "INSERT INTO messages (stream_id, wire_id, payload, headers) VALUES ( @stream{0}, @wire{0}, @payload{0}, @headers{0} );\n";
		private const string UpdateTransformationCheckpoint = "UPDATE checkpoints SET transformation = {0} WHERE transformation < {0};";
		private readonly List<WireMessage> buffer = new List<WireMessage>();
		private readonly ConnectionStringSettings settings;
		private long currentSequence;
		private long transformationCheckpoint;
	}
}