namespace Hydrospanner.Persistence.SqlStorage
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using System.Text;
	using Hydrospanner.Phases.Journal;

	public class SqlMessageStorage : IMessageStorage
	{
		public bool Save(List<JournalItem> items)
		{
			if (items == null || items.Count == 0)
				return true;

			try
			{
				this.buffer = items;
				this.TrySave();
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				this.Cleanup();
			}
		}
		private void TrySave()
		{
			using (this.connection = this.factory.OpenConnection(this.connectionString))
			using (this.transaction = this.connection.BeginTransaction(IsolationLevel.ReadCommitted))
			{
				this.Save();
				this.transaction.Commit();
			}
		}
		private void Save()
		{
			using (this.command = this.connection.CreateCommand())
			{
				this.command.Transaction = this.transaction;

				while (this.buffer.Count > this.index)
				{
					this.statementBuilder.Clear();
					this.parameters.Clear();
					this.command.Parameters.Clear();
					this.command.CommandText = null;

					this.index += this.Add();
					this.command.ExecuteNonQuery();
				}
			}
		}
		private int Add()
		{
			// TODO: add the maximum number of items possible and return the number added
			for (var i = 0; i < this.buffer.Count; i++)
			{
				var message = this.buffer[i];
				var statement = message.Acknowledgment == null ? this.AddForeignMessage(message, i) : this.AddLocalItem(message, i);
				this.statementBuilder.AppendLine(statement);
			}

			this.command.CommandText = this.statementBuilder.ToString();
			return this.buffer.Count;
		}
		private string AddForeignMessage(JournalItem item, int count)
		{
			int foreignIdIndex;

			if (!this.parameters.TryGetValue(item.ForeignId, out foreignIdIndex))
			{
				this.parameters[item.ForeignId] = foreignIdIndex = this.parameters.Count + 1;
				var id = this.command.CreateParameter();
				id.ParameterName = "@w" + foreignIdIndex;
				id.DbType = DbType.Guid;
				id.Value = item.ForeignId == Guid.Empty ? (object)DBNull.Value : item.ForeignId;
				this.command.Parameters.Add(id);
			}

			var payload = this.command.CreateParameter();
			payload.ParameterName = "@p" + count;
			payload.DbType = DbType.Binary;
			payload.Value = item.SerializedBody;
			this.command.Parameters.Add(payload);

			var headers = this.command.CreateParameter();
			headers.ParameterName = "@h" + count;
			headers.DbType = DbType.Binary;
			headers.Value = item.SerializedHeaders ?? (object)DBNull.Value;
			this.command.Parameters.Add(headers);

			return InsertForeignMessage.FormatWith(foreignIdIndex, count, count);
		}
		private string AddLocalItem(JournalItem item, int count)
		{
			var payload = this.command.CreateParameter();
			payload.ParameterName = "@p" + count;
			payload.DbType = DbType.Binary;
			payload.Value = item.SerializedBody;
			this.command.Parameters.Add(payload);

			var headers = this.command.CreateParameter();
			headers.ParameterName = "@p" + count;
			headers.DbType = DbType.Binary;
			headers.Value = item.SerializedHeaders ?? (object)DBNull.Value;
			this.command.Parameters.Add(headers);

			return InsertLocalMessage.FormatWith(count);
		}

		private void Cleanup()
		{
			this.command = this.command.TryDispose();
			this.transaction = this.transaction.TryDispose();
			this.connection = this.connection.TryDispose();
			this.parameters.Clear();
			this.statementBuilder.Clear();
			this.buffer = null;
			this.index = 0;
		}

		public SqlMessageStorage(ConnectionStringSettings settings)
			: this(DbProviderFactories.GetFactory(settings.ProviderName ?? "System.Data.SqlClient"), settings.ConnectionString)
		{
		}
		public SqlMessageStorage(DbProviderFactory factory, string connectionString)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			this.factory = factory;
			this.connectionString = connectionString;
		}

		private const string InsertForeignMessage = "INSERT INTO messages (wire_id,payload,headers) VALUES (@w{0},@p{1},@h{2})";
		private const string InsertLocalMessage = "INSERT INTO messages (payload,headers) VALUES (@p{0},@h{0})";
		private readonly Dictionary<Guid, int> parameters = new Dictionary<Guid, int>();
		private readonly StringBuilder statementBuilder = new StringBuilder(InsertForeignMessage.Length * 1024 * 32);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;

		private IDbConnection connection;
		private IDbTransaction transaction;
		private IDbCommand command;
		private List<JournalItem> buffer;
		private int index;
	}
}