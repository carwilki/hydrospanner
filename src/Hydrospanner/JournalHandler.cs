namespace Hydrospanner
{
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Text;
	using Disruptor;

	public class JournalHandler : IEventHandler<DispatchMessage>
	{
		public void OnNext(DispatchMessage data, long sequence, bool endOfBatch)
		{
			if (data.MessageSequence > 0)
				return; // this message has already been journaled

			this.buffer.Add(data);

			// TODO: don't limit buffer size here
			if (endOfBatch || this.buffer.Count >= 420)
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

					command.WithParameter("@wire{0}".FormatWith(i), item.WireId);
					command.WithParameter("@payload{0}".FormatWith(i), item.SerializedBody);
					command.WithParameter("@headers{0}".FormatWith(i), item.SerializedHeaders);
					builder.AppendFormat(AppendMessage, i);
				}

				command.Transaction = transaction;
				command.CommandText = builder.ToString();

				command.ExecuteNonQuery();
				transaction.Commit();

				this.buffer.Clear();
			}
		}

		public JournalHandler(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}

		private const string AppendMessage = "INSERT INTO messages (wire_id, payload, headers) SELECT @wire{0}, @payload{0}, @headers{0};\n";
		private readonly List<DispatchMessage> buffer = new List<DispatchMessage>();
		private readonly ConnectionStringSettings settings;
	}
}