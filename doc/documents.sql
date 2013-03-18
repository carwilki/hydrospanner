CREATE TABLE IF NOT EXISTS `hydrospanner`.`documents` (
	`identifier` VARCHAR(256) NOT NULL ,
	`message_sequence` BIGINT NOT NULL ,
	`document_hash` INT UNSIGNED NOT NULL ,
	`document` MEDIUMBLOB NULL ,
	PRIMARY KEY (`identifier`) ,
	UNIQUE INDEX `identifier_UNIQUE` (`identifier` ASC) 
) DEFAULT CHARACTER SET = latin1;