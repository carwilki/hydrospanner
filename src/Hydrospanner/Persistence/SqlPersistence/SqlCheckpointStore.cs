namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
	using System.Data.Common;

	public class SqlCheckpointStore : IDispatchCheckpointStore
	{
		public void Save(long sequence)
		{
			if (sequence <= 0)
				return;

			while (true)
			{
				try
				{
					this.TrySave(sequence);
					break;
				}
				catch
				{
					Timeout.Sleep();
				}	
			}
		}
		private void TrySave(long sequence)
		{
			using (var connection = this.factory.OpenConnection(this.connectionString))
			using (var command = connection.CreateCommand(SqlStatement.FormatWith(sequence)))
				command.ExecuteNonQuery();
		}

		public SqlCheckpointStore(DbProviderFactory factory, string connectionString)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException("connectionString");

			this.factory = factory;
			this.connectionString = connectionString;
		}

		private const string SqlStatement = @"UPDATE checkpoints SET dispatch = {0} WHERE dispatch < {0};";
		private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
		private readonly DbProviderFactory factory;
		private readonly string connectionString;
	}
}