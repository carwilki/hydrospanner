namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using System.Linq;
	using System.Text;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStore : IMessageStore
	{
		public void Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return;

			while (true)
			{
				try
				{
					this.TrySave(items);
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
		private void TrySave(List<JournalItem> items)
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var tranaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
			using (var command = tranaction.CreateCommand())
			{
				for (var i = 0; i < items.Count; i++)
					this.AddItem(command, items[i], i);

				command.CommandText = this.statementBuilder.ToString();
				command.ExecuteNonQuery();

				tranaction.Commit();

				this.registeredTypeCommittedIndex = this.registeredTypes.Count;
			}
		}
		private void AddItem(IDbCommand command, JournalItem item, int index)
		{
			var metadataId = this.RegisterType(item.SerializedType);
			if (metadataId == this.registeredTypes.Count)
			{
				command.AddParameter("@t{0}", metadataId, DbType.String, item.SerializedType);
				this.statementBuilder.AppendFormat(InsertType, metadataId);
			}

			var foreign = item.Acknowledgment != null;
			if (foreign)
				command.AddParameter("@f{0}", index, DbType.Guid, item.ForeignId);
			command.AddParameter("@p{0}", index, DbType.Binary, item.SerializedBody);
			command.AddParameter("@h{0}", index, DbType.Binary, item.SerializedHeaders);
			var statement = foreign ? InsertForeignMessage : InsertLocalMessage;
			this.statementBuilder.AppendFormat(statement, item.MessageSequence, metadataId, index);
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
		private readonly StringBuilder statementBuilder = new StringBuilder();
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private int registeredTypeCommittedIndex;
	}
}