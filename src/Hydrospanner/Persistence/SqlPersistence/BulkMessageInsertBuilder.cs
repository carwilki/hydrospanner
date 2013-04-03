namespace Hydrospanner.Persistence.SqlPersistence
{
	using System.Data;
	using System.Text;
	using Phases.Journal;

	public class BulkMessageInsertBuilder
	{
		public void NewInsert(IDbTransaction transaction)
		{
			this.index = 0;
			this.statement.Clear();
			this.command = transaction.CreateCommand();
		}

		public void Include(JournalItem item)
		{
			this.AddSerializedData(item);
			var metadataId = this.AddMetadata(item);
			var insert = this.DetermineInsertStatement(item);
			this.statement.AppendFormat(insert, item.MessageSequence, metadataId, this.index);
			this.index++;
		}
		private void AddSerializedData(JournalItem item)
		{
			this.command.AddParameter("@p{0}", this.index, DbType.Binary, item.SerializedBody);
			this.command.AddParameter("@h{0}", this.index, DbType.Binary, item.SerializedHeaders);
		}
		private short AddMetadata(JournalItem item)
		{
			short metadataId;
			if (!this.types.IsRegistered(item.SerializedType))
			{
				metadataId = this.types.Register(item.SerializedType);
				this.command.AddParameter("@t{0}", metadataId, DbType.String, item.SerializedType);
				this.statement.AppendFormat(InsertType, metadataId);
			}
			else
				metadataId = this.types.GetIdentifier(item.SerializedType);

			return metadataId;
		}
		private string DetermineInsertStatement(JournalItem item)
		{
			if (!item.ItemActions.HasFlag(JournalItemAction.Acknowledge))
				return InsertLocalMessage;

			this.command.AddParameter("@f{0}", this.index, DbType.Binary, item.ForeignId.ToByteArray());
			return InsertForeignMessage;
		}

		public IDbCommand Build()
		{
			this.command.CommandText = this.statement.ToString();
			return this.command;
		}

		public void Cleanup()
		{
			this.statement.Clear();
			this.types.DropPendingTypes();
		}

		public BulkMessageInsertBuilder(JournalMessageTypeRegistrar types)
		{
			this.types = types;
		}

		private const string InsertType = "INSERT INTO metadata SELECT {0}, @t{0};\n";
		private const string InsertLocalMessage = "INSERT INTO messages SELECT {0}, {1}, NULL, @p{2}, @h{2};\n";
		private const string InsertForeignMessage = "INSERT INTO messages SELECT {0}, {1}, @f{2}, @p{2}, @h{2};\n";
		private readonly JournalMessageTypeRegistrar types;
		private readonly StringBuilder statement = new StringBuilder();
		private IDbCommand command;
		int index;
	}
}