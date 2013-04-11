create database if not exists `<MESSAGES-DATABASE-NAME-HERE>`; # Substitute department name here...

use `<MESSAGES-DATABASE-NAME-HERE>`;

CREATE TABLE metadata (
	metadata_id smallint NOT NULL,
	type_name varchar(4096) NOT NULL,
    CONSTRAINT PK_metadata PRIMARY KEY CLUSTERED (metadata_id)
);

CREATE TABLE messages (
	sequence bigint NOT NULL,
	metadata_id smallint NOT NULL,
	foreign_id BINARY(16) NULL,
	payload mediumblob NOT NULL,
	headers mediumblob NULL,
    CONSTRAINT PK_checkpoints PRIMARY KEY CLUSTERED (sequence)
);

CREATE TABLE checkpoints (
	dispatch bigint NOT NULL,
    CONSTRAINT PK_checkpoints PRIMARY KEY CLUSTERED (dispatch)
);

INSERT INTO checkpoints SELECT 0;

CREATE FUNCTION toguid($guid binary(16)) RETURNS char(36) CHARSET utf8
    RETURN CONCAT(
        LOWER(HEX(SUBSTRING($guid,4,1))), LOWER(HEX(SUBSTRING($guid,3,1))),
        LOWER(HEX(SUBSTRING($guid,2,1))), LOWER(HEX(SUBSTRING($guid,1,1))), '-', 
        LOWER(HEX(SUBSTRING($guid,6,1))), LOWER(HEX(SUBSTRING($guid,5,1))), '-',
        LOWER(HEX(SUBSTRING($guid,8,1))), LOWER(HEX(SUBSTRING($guid,7,1))), '-',
        LOWER(HEX(SUBSTRING($guid,9,2))), '-', LOWER(HEX(SUBSTRING($guid,11,6))));

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

CREATE VIEW `messages_view` AS
SELECT M.sequence, T.type_name, toguid(M.foreign_id), CAST(M.payload as CHAR(65535)), CAST(M.headers as CHAR(65535))
  FROM messages M
  JOIN metadata T on M.metadata_id = T.metadata_id;
