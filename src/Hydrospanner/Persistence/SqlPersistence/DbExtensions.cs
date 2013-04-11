namespace Hydrospanner.Persistence.SqlPersistence
{
	using System;
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

		public static IDbCommand CreateCommand(this IDbConnection connection, string statement = null)
		{
			var command = connection.CreateCommand();
			command.CommandText = statement;
			return command;
		}

		public static void AddParameter(this IDbCommand command, string name, object value, DbType type)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = name;
			parameter.DbType = type;
			parameter.Value = value ?? DBNull.Value;
			command.Parameters.Add(parameter);
		}

		public static Guid ToGuid(this byte[] value)
		{
			return value == null || value.Length == 0 ? Guid.Empty : new Guid(value);
		}
	}
}