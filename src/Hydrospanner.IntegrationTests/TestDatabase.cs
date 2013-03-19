#pragma warning disable 169
// ReSharper disable InconsistentNaming

namespace Hydrospanner.IntegrationTests
{
	using System;
	using System.Configuration;
	using System.Data;
	using Machine.Specifications;

	public class TestDatabase
	{
		Establish context = () =>
		{
			ThreadExtensions.Freeze(x => napTime = x);

			settings = ConfigurationManager.ConnectionStrings["DB"];
			connection = ConfigurationManager.ConnectionStrings["DB-startup"].OpenConnection();
			using (var command = connection.CreateCommand())
			{
				command.CommandText = Initiailize;
				command.ExecuteNonQuery();
			}
		};

		Cleanup after = () =>
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = Cleanup;
				command.ExecuteNonQuery();
			}
			connection.Close();
			connection.Dispose();
			connection = null;
			settings = null;
		};

		protected static TimeSpan napTime;
		protected static IDbConnection connection;
		protected static ConnectionStringSettings settings;
		const string Initiailize = @"
			CREATE DATABASE IF NOT EXISTS `hydrospanner-test`;
			CREATE TABLE IF NOT EXISTS `hydrospanner-test`.`documents` (
				`identifier` VARCHAR(256) NOT NULL ,
				`message_sequence` BIGINT NOT NULL ,
				`document_hash` INT UNSIGNED NOT NULL ,
				`document` MEDIUMBLOB NULL ,
				PRIMARY KEY (`identifier`) ,
				UNIQUE INDEX `identifier_UNIQUE` (`identifier` ASC) 
			) DEFAULT CHARACTER SET = latin1;
			TRUNCATE TABLE `hydrospanner-test`.`documents`;";
		const string Cleanup = @"drop database `hydrospanner-test`;";
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169