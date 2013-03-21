namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;

	public sealed class SqlBootstrapStore : IBootstrapStore
	{
		public BootstrapInfo Load()
		{
			while (true)
			{
				try
				{
					var loaded = this.TryLoad();
					if (loaded.Populated)
						return loaded;
				}
				catch
				{
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
			if (reader == null)
				return new BootstrapInfo();

			long journaled = 0, dispatched = 0;
			var types = new List<string>();
			var identifiers = new List<Guid>(this.duplicateWindow);

			if (reader.Read())
			{
				journaled = reader.GetInt64(0);
				dispatched = reader.GetInt64(0);
			}

			if (reader.NextResult())
				while (reader.Read())
					types.Add(reader.GetString(0));

			if (reader.NextResult())
				while (reader.Read())
				{
					Guid id;
					if (Guid.TryParse(reader.GetString(0), out id))
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
			SELECT COALESCE(MAX(sequence), 0) AS sequence, MAX(dispatch) AS dispatch FROM checkpoints LEFT OUTER JOIN messages ON 1=1;
			SELECT type_name FROM metadata ORDER BY metadata_id;
		    SELECT toguid(foreign_id) FROM messages WHERE foreign_id IS NOT NULL ORDER BY sequence LIMIT {0};";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
		private readonly int duplicateWindow;
	}
}