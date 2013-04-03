namespace Hydrospanner.Persistence.SqlPersistence
{
	using System.Text;
	using Phases.Journal;

	public class SqlBulkInsertCommandBuilder
	{
		public virtual void NewBatch()
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
			this.session.IncludeParameter("@p{0}".FormatWith(this.index), item.SerializedBody);
			this.session.IncludeParameter("@h{0}".FormatWith(this.index), item.SerializedHeaders);
		}
		private short AddMetadata(JournalItem item)
		{
			short metadataId;
			if (!this.types.IsRegistered(item.SerializedType))
			{
				metadataId = this.types.Register(item.SerializedType);
				this.session.IncludeParameter("@t{0}".FormatWith(metadataId), item.SerializedType);
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

			this.session.IncludeParameter("@f{0}".FormatWith(this.index), item.ForeignId.ToByteArray());
			return InsertForeignMessage;
		}

		public virtual string Build()
		{
			return this.statement.ToString();
		}

		public virtual void Cleanup()
		{
			this.statement.Clear();
			this.types.DropPendingTypes();
		}

		public SqlBulkInsertCommandBuilder(JournalMessageTypeRegistrar types, SqlBulkInsertSession session)
		{
			this.types = types;
			this.session = session;
		}
		protected SqlBulkInsertCommandBuilder()
		{
		}

		private const string InsertType = "INSERT INTO metadata SELECT {0}, @t{0};\n";
		private const string InsertLocalMessage = "INSERT INTO messages SELECT {0}, {1}, NULL, @p{2}, @h{2};\n";
		private const string InsertForeignMessage = "INSERT INTO messages SELECT {0}, {1}, @f{2}, @p{2}, @h{2};\n";
		private readonly StringBuilder statement = new StringBuilder();
		private readonly JournalMessageTypeRegistrar types;
		private readonly SqlBulkInsertSession session;
		private int index;
	}
}