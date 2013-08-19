#pragma warning disable 169, 414
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Configuration;
	using System.Data;
	using System.Data.Common;
	using Machine.Specifications;
	using Persistence.SqlPersistence;

	public class TestDatabase
	{
		Establish context = () =>
		{
			ThreadExtensions.Freeze(x => napTime = x);

			settings = ConfigurationManager.ConnectionStrings["DB"];
			factory = DbProviderFactories.GetFactory(settings.ProviderName);
			connectionString = settings.ConnectionString;
			connection = factory.OpenConnection(connectionString);
			InitializeDatabase();
		};

		protected static void InitializeDatabase()
		{
			var statement = ("DROP DATABASE IF EXISTS `{0}`; CREATE DATABASE `{0}`;"
					+ DbScripts.MessagesCreation
					+ DbScripts.DocumentsCreation).FormatWith(DbName);

			using (var command = connection.CreateCommand(statement))
				command.ExecuteNonQuery();
		}

		Cleanup after = () =>
		{
			TearDownDatabase();
			connection.Close();
			connection.Dispose();
			connection = null;
			settings = null;
			factory = null;
			connectionString = null;
		};

		protected static void TearDownDatabase()
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = Cleanup.FormatWith(DbName);
				command.ExecuteNonQuery();
			}
		}

		protected static TimeSpan napTime;
		protected static IDbConnection connection;
		protected static ConnectionStringSettings settings;
		protected static DbProviderFactory factory;
		protected static string connectionString;
		const string DbName = "hydrospanner-test";
		const string Cleanup = @"DROP TABLE `messages`; DROP TABLE `checkpoints`; DROP TABLE `documents`; DROP TABLE `metadata`;";
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414