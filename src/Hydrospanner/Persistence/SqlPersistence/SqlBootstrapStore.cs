namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Data.Common;

	public class SqlBootstrapStore : IBootstrapStore
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
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var command = connection.CreateCommand(SqlStatement))
			using (var reader = command.ExecuteReader())
				return Parse(reader);
		}
		private static BootstrapInfo Parse(IDataReader reader)
		{
			if (reader == null)
				return new BootstrapInfo();

			long journaled = 0, dispatched = 0;
			var types = new List<string>();

			if (reader.Read())
			{
				journaled = reader.GetInt64(0);
				dispatched = reader.GetInt64(0);
			}

			if (reader.NextResult())
				while (reader.Read())
					types.Add(reader.GetString(0));

			return new BootstrapInfo(journaled, dispatched, types);
		}

		public SqlBootstrapStore(DbProviderFactory factory, string connectionString)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			this.factory = factory;
			this.connectionString = connectionString;
		}

		private const string SqlStatement = @"
			SELECT COALESCE(MAX(sequence), 0) AS sequence, MAX(dispatch) AS dispatch FROM checkpoints LEFT OUTER JOIN messages ON 1=1;
			SELECT type_name FROM metadata ORDER BY id;";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
	}
}