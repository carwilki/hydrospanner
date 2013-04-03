namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Data;
	using System.Data.Common;

	public class SqlBulkInsertSession : IDisposable
	{
		public virtual SqlBulkInsertSession BeginNewSession()
		{
			this.connection = this.factory.OpenConnection(this.connectionString);
			this.transaction = this.connection.BeginTransaction(IsolationLevel.ReadCommitted);
			return this;
		}

		public virtual void PrepareNewCommand()
		{
			this.command = this.transaction.Connection.CreateCommand();
			command.Transaction = this.transaction;
		}

		public virtual void IncludeParameter(string name, object value)
		{
			var type = value is byte[] ? DbType.Binary : DbType.String;
			this.command.AddParameter(name, value, type);
		}

		public virtual void ExecuteCurrentCommand(string commandText)
		{
			this.command.CommandText = commandText;
			this.command.ExecuteNonQuery();
		}

		public virtual void CommitTransaction()
		{
			this.transaction.Commit();
			this.Dispose(true);
		}

		public SqlBulkInsertSession(DbProviderFactory factory, string connectionString)
		{
			this.factory = factory;
			this.connectionString = connectionString;

		}
		protected SqlBulkInsertSession()
		{
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
				return;

			this.connection = this.connection.TryDispose();
			this.transaction = this.transaction.TryDispose();
			this.command = this.command.TryDispose();
		}

		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private IDbConnection connection;
		private IDbTransaction transaction;
		private IDbCommand command;
	}
}