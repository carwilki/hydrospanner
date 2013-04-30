create database if not exists `<DOCUMENTS-DATABASE-NAME-HERE>`; # Substitute department name here...

use `<DOCUMENTS-DATABASE-NAME-HERE>`;


CREATE TABLE IF NOT EXISTS documents (
	`identifier` VARCHAR(1024) NOT NULL,
	`message_sequence` BIGINT NOT NULL,
	`document_hash` INT UNSIGNED NOT NULL,
	`document` MEDIUMBLOB NULL,
	PRIMARY KEY (`identifier`),
	UNIQUE INDEX `identifier_UNIQUE` (`identifier` ASC) 
) DEFAULT CHARACTER SET = latin1;


CREATE VIEW `documents_view` AS
SELECT identifier, CAST(document as char(65535)) as document, message_sequence, document_hash
  FROM documents;
