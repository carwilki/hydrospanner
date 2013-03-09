namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;

	public class MessageStore
	{
		public long LoadJournalCheckpoint()
		{
			return this.LoadSequence("SELECT MAX(sequence) FROM messages;");
		}
		public void UpdateDispatchCheckpoint(long sequence)
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "UPDATE checkpoints SET dispatch = {0} WHERE dispatch < {0};".FormatWith(sequence);
				command.ExecuteNonQuery();
			}
		}
		public long LoadDispatchCheckpoint()
		{
			return this.LoadSequence("SELECT dispatch FROM checkpoints;");
		}
		private long LoadSequence(string commandText)
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = commandText;
				var value = command.ExecuteScalar();
				return value is long ? (long)value : 0;
			}
		}

		public IEnumerable<Guid> LoadWireIdentifiers(long maxSequence, int count)
		{
			if (count <= 0 || maxSequence < 0)
				yield break;

			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT wire_id FROM messages WHERE sequence >= {0};".FormatWith(maxSequence - count);
				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						yield break;

					while (reader.Read())
						yield return reader.GetGuid(0);
				}
			}
		}
		public IEnumerable<JournaledMessage> LoadSinceCheckpoint(long checkpoint)
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = "SELECT sequence, wire_id, payload, headers FROM messages WHERE sequence > {0};".FormatWith(checkpoint);
				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						yield break;

					while (reader.Read())
						yield return new JournaledMessage
						{
							MessageSequence = reader.GetInt64(0),
							WireId = reader.GetGuid(1),
							SerializedBody = reader[2] as byte[],
							SerializedHeaders = reader[3] as byte[]
						};
				}
			}
		}

		public MessageStore(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}

		private readonly ConnectionStringSettings settings;
	}
	
	public sealed class JournaledMessage
	{
		public long MessageSequence { get; set; }
		public Guid WireId { get; set; }
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}