SET @migrate := (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'auth'
      AND table_name = 'UsersExternalLogins'
      AND column_name = 'UserExternalLoginId'
      AND data_type = 'bigint'
);

SET @sql := IF(@migrate > 0,
    'CREATE TABLE IF NOT EXISTS `auth`.`_UserExternalLoginIdMap` (`OldId` BIGINT NOT NULL PRIMARY KEY, `NewId` CHAR(36) NOT NULL)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'TRUNCATE TABLE `auth`.`_UserExternalLoginIdMap`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'INSERT INTO `auth`.`_UserExternalLoginIdMap` (`OldId`, `NewId`) SELECT `UserExternalLoginId`, UUID() FROM `auth`.`UsersExternalLogins`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'UPDATE `auth`.`Audits` a INNER JOIN `auth`.`_UserExternalLoginIdMap` m ON a.`EntityId` = CAST(m.`OldId` AS CHAR) SET a.`EntityId` = m.`NewId` WHERE a.`EntityType` = 6',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'UPDATE `auth`.`UsersCommunicationEndpoints` e INNER JOIN `auth`.`UsersExternalLogins` el ON e.`UserAccountId` = el.`UserAccountId` INNER JOIN `auth`.`_UserExternalLoginIdMap` m ON el.`UserExternalLoginId` = m.`OldId` SET e.`EntityId` = m.`NewId`, e.`UpdatedAt` = UTC_TIMESTAMP() WHERE e.`Source` = 1 AND e.`Channel` = 0 AND el.`ProviderEmail` IS NOT NULL AND LOWER(TRIM(e.`Address`)) = LOWER(TRIM(el.`ProviderEmail`)) AND (e.`EntityId` IS NULL OR e.`EntityId` <> m.`NewId`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` ADD COLUMN `UserExternalLoginIdNew` CHAR(36) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'UPDATE `auth`.`UsersExternalLogins` el INNER JOIN `auth`.`_UserExternalLoginIdMap` m ON el.`UserExternalLoginId` = m.`OldId` SET el.`UserExternalLoginIdNew` = m.`NewId`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` DROP PRIMARY KEY',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` DROP COLUMN `UserExternalLoginId`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` CHANGE COLUMN `UserExternalLoginIdNew` `UserExternalLoginId` CHAR(36) NOT NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` ADD CONSTRAINT `PK_auth_UsersExternalLogins` PRIMARY KEY (`UserExternalLoginId`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @rename_last_used := (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = 'auth'
      AND table_name = 'UsersExternalLogins'
      AND column_name = 'LastUsedAt'
      AND NOT EXISTS (
          SELECT 1
          FROM information_schema.columns c2
          WHERE c2.table_schema = 'auth'
            AND c2.table_name = 'UsersExternalLogins'
            AND c2.column_name = 'UpdatedAt'
      )
);

SET @sql := IF(@migrate > 0 AND @rename_last_used > 0,
    'ALTER TABLE `auth`.`UsersExternalLogins` CHANGE COLUMN `LastUsedAt` `UpdatedAt` DATETIME(6) NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(@migrate > 0,
    'DROP TABLE `auth`.`_UserExternalLoginIdMap`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
