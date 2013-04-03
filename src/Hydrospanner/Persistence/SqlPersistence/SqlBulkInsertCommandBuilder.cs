namespace Hydrospanner.Persistence.SqlPersistence
{
	using System.Data;
	using System.Text;
	using Phases.Journal;

	public class SqlBulkInsertCommandBuilder
	{
		public void NewInsert()
		{
			this.index = 0;
			this.statement.Clear();
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
			this.session.IncludeParameter("@p{0}", this.index, item.SerializedBody, DbType.Binary);
			this.session.IncludeParameter("@h{0}", this.index, item.SerializedHeaders, DbType.Binary);
		}
		private short AddMetadata(JournalItem item)
		{
			short metadataId;
			if (!this.types.IsRegistered(item.SerializedType))
			{
				metadataId = this.types.Register(item.SerializedType);
				this.session.IncludeParameter("@t{0}", metadataId, item.SerializedType, DbType.String);
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

			this.session.IncludeParameter("@f{0}", this.index, item.ForeignId.ToByteArray(), DbType.Binary);
			return InsertForeignMessage;
		}

		public string Build()
		{
			return this.statement.ToString();
		}

		public void Cleanup()
		{
			this.statement.Clear();
			this.types.DropPendingTypes();
		}

		public SqlBulkInsertCommandBuilder(JournalMessageTypeRegistrar types, SqlBulkInsertSession session)
		{
			this.types = types;
			this.session = session;
		}

		private const string InsertType = "INSERT INTO metadata SELECT {0}, @t{0};\n";
		private const string InsertLocalMessage = "INSERT INTO messages SELECT {0}, {1}, NULL, @p{2}, @h{2};\n";
		private const string InsertForeignMessage = "INSERT INTO messages SELECT {0}, {1}, @f{2}, @p{2}, @h{2};\n";
		private readonly StringBuilder statement = new StringBuilder();
		private readonly JournalMessageTypeRegistrar types;
		private readonly SqlBulkInsertSession session;
		int index;
	}
}