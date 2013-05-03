#pragma warning disable 169, 414
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
			InitializeDatabase();
		};

		protected static void InitializeDatabase()
		{
			using (var command = connection.CreateCommand())
			{
				command.CommandText = Initialize;
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
		const string Initialize = @"
			DROP DATABASE IF EXISTS `hydrospanner-test`;
			CREATE DATABASE `hydrospanner-test`;
			USE `hydrospanner-test`;

			CREATE TABLE IF NOT EXISTS metadata (
				metadata_id smallint NOT NULL,
				type_name varchar(4096) NOT NULL,
				CONSTRAINT PK_metadata PRIMARY KEY CLUSTERED (metadata_id)
			);

			CREATE TABLE IF NOT EXISTS messages (
				sequence bigint NOT NULL,
				metadata_id smallint NOT NULL,
				foreign_id BINARY(16) NULL,
				payload mediumblob NOT NULL,
				headers mediumblob NULL,
				CONSTRAINT PK_checkpoints PRIMARY KEY CLUSTERED (sequence)
			);

			CREATE TABLE IF NOT EXISTS checkpoints (
				dispatch bigint NOT NULL,
				CONSTRAINT PK_checkpoints PRIMARY KEY CLUSTERED (dispatch)
			);
			INSERT INTO checkpoints SELECT 0 ON DUPLICATE KEY UPDATE dispatch = 0;

			CREATE TABLE IF NOT EXISTS documents (
				`identifier` VARCHAR(256) NOT NULL,
				`sequence` BIGINT NOT NULL,
				`hash` INT UNSIGNED NOT NULL,
				`document` MEDIUMBLOB NULL,
				PRIMARY KEY (`identifier`),
				UNIQUE INDEX `identifier_UNIQUE` (`identifier` ASC) 
			) DEFAULT CHARACTER SET = latin1;

			DROP FUNCTION IF EXISTS toguid;
			CREATE FUNCTION toguid($guid binary(16)) RETURNS char(36) CHARSET utf8
				RETURN CONCAT(
					LOWER(HEX(SUBSTRING($guid,4,1))), LOWER(HEX(SUBSTRING($guid,3,1))),
					LOWER(HEX(SUBSTRING($guid,2,1))), LOWER(HEX(SUBSTRING($guid,1,1))), '-', 
					LOWER(HEX(SUBSTRING($guid,6,1))), LOWER(HEX(SUBSTRING($guid,5,1))), '-',
					LOWER(HEX(SUBSTRING($guid,8,1))), LOWER(HEX(SUBSTRING($guid,7,1))), '-',
					LOWER(HEX(SUBSTRING($guid,9,2))), '-', LOWER(HEX(SUBSTRING($guid,11,6))));

			DROP FUNCTION IF EXISTS tobin;
			CREATE FUNCTION tobin($guid char(36)) RETURNS binary(16)
				RETURN CONCAT(
					UNHEX(SUBSTRING($guid, 7,  2)),
					UNHEX(SUBSTRING($guid, 5,  2)),
					UNHEX(SUBSTRING($guid, 3,  2)),
					UNHEX(SUBSTRING($guid, 1,  2)),
					UNHEX(SUBSTRING($guid, 12, 2)),
					UNHEX(SUBSTRING($guid, 10, 2)),
					UNHEX(SUBSTRING($guid, 17, 2)),
					UNHEX(SUBSTRING($guid, 15, 2)),
					UNHEX(SUBSTRING($guid, 20, 4)),
					UNHEX(SUBSTRING($guid, 25, 12)));

			DROP VIEW IF EXISTS `documents_view`;
			CREATE VIEW `documents_view` AS
			SELECT identifier, CAST(document as char(65535)), sequence, hash
			  FROM documents;

			DROP VIEW IF EXISTS `messages_view`;
			CREATE VIEW `messages_view` AS
			SELECT M.sequence, T.type_name, toguid(M.foreign_id), CAST(M.payload as CHAR(65535)), CAST(M.headers as CHAR(65535))
			  FROM messages M
			  JOIN metadata T on M.metadata_id = T.metadata_id;";
		const string Cleanup = @"DROP DATABASE IF EXISTS `hydrospanner-test`;";
	}
}

// ReSharper restore InconsistentNaming
#pragma warning restore 169, 414