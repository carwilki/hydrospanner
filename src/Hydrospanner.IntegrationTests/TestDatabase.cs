#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Configuration;
	using System.Data;
	using Machine.Specifications;
	using Persistence.SqlPersistence;

	public class TestDatabase
	{
		Establish context = () =>
		{
			ThreadExtensions.Freeze(x => napTime = x);

			settings = ConfigurationManager.ConnectionStrings["DB"];
			connection = ConfigurationManager.ConnectionStrings["DB-startup"].OpenConnection();
			InitializeDatabase();
		};

		protected static void InitializeDatabase()
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = 
					("DROP DATABASE IF EXISTS `{0}`; CREATE DATABASE `{0}`;"
					+ DbScripts.MessagesCreation
					+ DbScripts.DocumentsCreation).FormatWith(DbName);
				command.ExecuteNonQuery();
			}
		}

		Cleanup after = () =>
		{
			TearDownDatabase();
			connection.Close();
			connection.Dispose();
			connection = null;
			settings = null;
		};

		protected static void TearDownDatabase()
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = Cleanup;
				command.ExecuteNonQuery();
			}
		}

		protected static TimeSpan napTime;
		protected static IDbConnection connection;
		protected static ConnectionStringSettings settings;
		const string DbName = "hydrospanner-test";
		const string Cleanup = @"DROP DATABASE IF EXISTS `hydrospanner-test`;";
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414