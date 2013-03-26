namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Text;
	using Phases.Journal;
	using Wireup;

	public sealed class SqlMessageStore : IMessageStore
	{
		public IEnumerable<JournaledMessage> Load(long startingSequence)
		{
			var types = this.registeredTypes.OrderBy(x => x.Value).Select(x => x.Key).ToArray();
			return new SqlMessageStoreReader(this.factory, this.connectionString, types, startingSequence);
		}

		public void Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return;

			while (true)
			{
				try
				{
					this.TrySave(items);
					break;
				}
				catch
				{
					Timeout.Sleep();
				}
				finally
				{
					this.Cleanup();
				}
			}
		}
		private void TrySave(IList<JournalItem> items)
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			using (var command = this.BuildCommand(items, transaction))
			{
				command.ExecuteNonQuery();
				transaction.Commit();
				this.registeredTypeCommittedIndex = this.registeredTypes.Count;
			}
		}

		IDbCommand BuildCommand(IList<JournalItem> items, IDbTransaction transaction)
		{
			var command = transaction.CreateCommand();
			for (var i = 0; i < items.Count; i++)
				this.AddItem(command, items[i], i);

			command.CommandText = this.statementBuilder.ToString();
			
			return command;
		}
		private void AddItem(IDbCommand command, JournalItem item, int index)
		{
			AddSerializedData(command, item, index);
			var metadataId = this.AddMetadata(command, item);
			var statement = DetermineInsertStatement(command, item, index);
			
			this.statementBuilder.AppendFormat(statement, item.MessageSequence, metadataId, index);
		}
		private static void AddSerializedData(IDbCommand command, JournalItem item, int index)
		{
			command.AddParameter("@p{0}", index, DbType.Binary, item.SerializedBody);
			command.AddParameter("@h{0}", index, DbType.Binary, item.SerializedHeaders);
		}
		private short AddMetadata(IDbCommand command, JournalItem item)
		{
			var metadataId = this.RegisterType(item.SerializedType);
			if (metadataId > this.registeredTypeCommittedIndex && !this.typesPendingRegistration.Contains(metadataId))
			{
				command.AddParameter("@t{0}", metadataId, DbType.String, item.SerializedType);
				this.statementBuilder.AppendFormat(InsertType, metadataId);
				this.typesPendingRegistration.Add(metadataId);
			}

			return metadataId;
		}
		private static string DetermineInsertStatement(IDbCommand command, JournalItem item, int index)
		{
			if (item.ItemActions.HasFlag(JournalItemAction.Acknowledge))
			{
				command.AddParameter("@f{0}", index, DbType.Binary, item.ForeignId.ToByteArray());
				return InsertForeignMessage;
			}

			return InsertLocalMessage;
		}

		private short RegisterType(string type)
		{
			short id;
			if (!this.registeredTypes.TryGetValue(type, out id))
				this.registeredTypes[type] = id = (short)(this.registeredTypes.Count + 1);

			return id;
		}
		private void Cleanup()
		{
			this.statementBuilder.Clear();
			this.typesPendingRegistration.Clear();

			if (this.registeredTypeCommittedIndex == this.registeredTypes.Count)
				return;

			var keys = this.registeredTypes.Where(x => x.Value > this.registeredTypeCommittedIndex).Select(x => x.Key).ToArray();
			foreach (var key in keys)
				this.registeredTypes.Remove(key);
		}

		public SqlMessageStore(DbProviderFactory factory, string connectionString, IEnumerable<string> types)
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
				this.registeredTypeCommittedIndex = this.RegisterType(type);
		}

		private const string InsertType = "INSERT INTO metadata SELECT {0}, @t{0};\n";
		private const string InsertLocalMessage = "INSERT INTO messages SELECT {0}, {1}, NULL, @p{2}, @h{2};\n";
		private const string InsertForeignMessage = "INSERT INTO messages SELECT {0}, {1}, @f{2}, @p{2}, @h{2};\n";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);
		private readonly Dictionary<string, short> registeredTypes = new Dictionary<string, short>(1024);
		private readonly HashSet<short> typesPendingRegistration = new HashSet<short>(); 
		private readonly StringBuilder statementBuilder = new StringBuilder();
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private int registeredTypeCommittedIndex;
	}
}