namespace Hydrospanner.Persistence.SqlMessageStore
{
	using System.Configuration;
	using System.Data;
	using System.Data.Common;

	internal static class DbExtensions
	{
		public static IDbConnection OpenConnection(this DbProviderFactory factory, string connectionString)
		{
			var connection = factory.CreateConnection();
			if (connection == null)
				throw new ConfigurationErrorsException("Unable to initialize database.");

			try
			{
				connection.ConnectionString = connectionString;
				connection.Open();
				return connection;
			}
			catch
			{
				connection.Dispose();
				throw;
			}
		}
		public static IDbCommand CreateCommand(this IDbTransaction transaction, string statement = null)
		{
			var command = transaction.Connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = statement;
			return command;
		}
	}
}