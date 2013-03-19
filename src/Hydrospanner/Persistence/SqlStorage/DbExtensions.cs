namespace Hydrospanner.Persistence.SqlStorage
{
	using System.Data;
	using System.Data.Common;

	internal static class DbExtensions
	{
		public static IDbConnection OpenConnection(this DbProviderFactory factory, string connectionString)
		{
			var connection = factory.CreateConnection();
			if (connection == null)
				return null;

			try
			{
				connection.ConnectionString = connectionString;
				connection.Open();
				return connection;
			}
			catch
			{
				connection.Dispose();
				return null;
			}
		}
	}
}