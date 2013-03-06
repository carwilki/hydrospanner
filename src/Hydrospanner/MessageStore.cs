﻿namespace Hydrospanner
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;

	public class MessageStore
	{
		public IList<Guid> LoadWireIdentifiers(int count)
		{
			if (count <= 0)
				return new Guid[0];

			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = IdentifiersUpToCheckpoint.FormatWith(count);
				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						return new Guid[0];

					var identifiers = new List<Guid>(count);

					while (reader.Read())
						identifiers.Add(reader.GetGuid(0));

					return identifiers;
				}
			}
		}

		public IList<JournaledMessage> LoadSinceCheckpoint()
		{
			using (var connection = this.settings.OpenConnection())
			using (var command = connection.CreateCommand())
			{
				command.CommandText = MessagesSinceCheckpoint.FormatWith();
				using (var reader = command.ExecuteReader())
				{
					if (reader == null)
						return new JournaledMessage[0];

					var messages = new List<JournaledMessage>(Math.Max(0, reader.RecordsAffected));

					while (reader.Read())
						messages.Add(new JournaledMessage
						{
							WireId = reader.GetGuid(0),
							SerializedBody = reader[1] as byte[],
							SerializedHeaders = reader[2] as byte[]
						});

					return messages;
				}
			}
		}

		public MessageStore(ConnectionStringSettings settings)
		{
			this.settings = settings;
		}

		private const string IdentifiersUpToCheckpoint = "SELECT wire_id FROM messages WHERE sequence BETWEEN (SELECT MAX(sequence) FROM checkpoints) AND (SELECT MAX(sequence) - {0} FROM checkpoints) AND wire_id IS NOT NULL;";
		private const string MessagesSinceCheckpoint = "SELECT wire_id, payload, headers FROM messages WHERE sequence > (SELECT sequence FROM checkpoints);";
		private readonly ConnectionStringSettings settings;
		
	}
	
	public sealed class JournaledMessage
	{
		public Guid WireId { get; set; }
		public byte[] SerializedBody { get; set; }
		public byte[] SerializedHeaders { get; set; }
	}
}