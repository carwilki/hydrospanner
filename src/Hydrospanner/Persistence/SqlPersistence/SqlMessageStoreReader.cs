namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data.Common;
	using Hydrospanner.Phases.Bootstrap;

	public class SqlMessageStoreReader
	{
		public IEnumerable<JournaledMessage> Load(long startingSequence)
		{
			this.currentSequence = startingSequence;

			while (true)
			{
				try
				{
					return this.TryLoad();
				}
				catch
				{
					Timeout.Sleep();
				}
			}
		}
		private IEnumerable<JournaledMessage> TryLoad()
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var command = connection.CreateCommand(LoadFromSequence.FormatWith(this.currentSequence)))
			using (var reader = command.ExecuteReader())
			{
				if (reader == null)
					yield break;

				while (reader.Read())
				{
					yield return new JournaledMessage
					{
						Sequence = this.currentSequence++,
						SerializedType = this.registeredTypes[reader.GetInt16(0)],
						ForeignId = reader.IsDBNull(1) ? Guid.Empty : reader.GetGuid(1),
						SerializedBody = reader[2] as byte[],
						SerializedHeaders = reader[3] as byte[],
					};
				}
			}
		}

		public SqlMessageStoreReader(DbProviderFactory factory, string connectionString, IEnumerable<string> types)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (types == null)
				throw new ArgumentNullException("types");

			this.factory = factory;
			this.connectionString = connectionString;
			foreach (var type in types)
				this.registeredTypes[(short)(this.registeredTypes.Count + 1)] = type;
		}

		private const string LoadFromSequence = @"SELECT metadata_id, foreign_id, payload, headers FROM messages WHERE sequence >= {0};";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly Dictionary<short, string> registeredTypes = new Dictionary<short, string>(1024);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private long currentSequence;
	}
}