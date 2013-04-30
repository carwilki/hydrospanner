namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;
	using log4net;

	public sealed class SqlBootstrapStore : IBootstrapStore
	{
		public BootstrapInfo Load()
		{
			while (true)
			{
				try
				{
					return this.TryLoad();
				}
				catch (Exception e)
				{
					Log.Warn("Unable to connect to message store; retrying in a few seconds.", e);
					Timeout.Sleep();
				}	
			}
		}
		private BootstrapInfo TryLoad()
		{
			var statment = SqlStatement.FormatWith(this.duplicateWindow);
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var command = connection.CreateCommand(statment))
			using (var reader = command.ExecuteReader())
				return this.Parse(reader);
		}
		private BootstrapInfo Parse(IDataReader reader)
		{
			long journaled = 0, dispatched = 0;
			var types = new List<string>(1024);
			var identifiers = new List<Guid>(this.duplicateWindow);

			if (reader.Read())
				journaled = reader.GetInt64(0);

			if (reader.NextResult() && reader.Read())
				dispatched = reader.GetInt64(0);

			if (reader.NextResult())
				while (reader.Read())
					types.Add(reader.GetString(0));

			if (reader.NextResult())
				while (reader.Read())
				{
					var id = new Guid(reader.GetValue(0) as byte[] ?? new byte[16]);
					if (id != Guid.Empty)
						identifiers.Add(id);
				}

			return new BootstrapInfo(journaled, dispatched, types, identifiers);
		}

		public SqlBootstrapStore(DbProviderFactory factory, string connectionString, int duplicateWindow)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			if (duplicateWindow <= 0)
				throw new ArgumentOutOfRangeException("duplicateWindow");

			this.factory = factory;
			this.connectionString = connectionString;
			this.duplicateWindow = duplicateWindow;
		}

		private const string SqlStatement = @"
		    SELECT sequence FROM messages ORDER BY sequence DESC LIMIT 1;
			SELECT MAX(dispatch) FROM checkpoints;
			SELECT type_name FROM metadata ORDER BY metadata_id;
		    SELECT foreign_id FROM messages WHERE foreign_id IS NOT NULL ORDER BY sequence DESC LIMIT {0};";
		private static readonly ILog Log = LogManager.GetLogger(typeof(SqlBootstrapStore));
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly int duplicateWindow;
	}
}