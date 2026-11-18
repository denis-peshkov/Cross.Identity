-- Create auth.AccountMergePendings (Cross.Identity account merge pending token).
-- Idempotent for existing DBs; journals matching 2_Initial script when table is present.

CREATE TABLE IF NOT EXISTS `auth`.`AccountMergePendings`
(
    `AccountMergePendingId` CHAR(36)     NOT NULL,
    `MergeToken`            VARCHAR(64)  NOT NULL,
    `SurvivorUserAccountId` CHAR(36)     NOT NULL,
    `AbsorbedUserAccountId` CHAR(36)     NOT NULL,
    `ExpiresAt`             DATETIME(6)  NOT NULL,
    `CreatedAt`             DATETIME(6)  NOT NULL,
    `ConsumedAt`            DATETIME(6)  NULL,
    `ConcurrencyStamp`      CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_AccountMergePendings` PRIMARY KEY (`AccountMergePendingId`),
    CONSTRAINT `FK_auth_AccountMergePendings_Survivor`
        FOREIGN KEY (`SurvivorUserAccountId`) REFERENCES `auth`.`UsersAccounts` (`UserAccountId`),
    CONSTRAINT `FK_auth_AccountMergePendings_Absorbed`
        FOREIGN KEY (`AbsorbedUserAccountId`) REFERENCES `auth`.`UsersAccounts` (`UserAccountId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

SET @ux_merge_token_exists := (
    SELECT COUNT(1)
    FROM information_schema.statistics
    WHERE table_schema = 'auth'
      AND table_name = 'AccountMergePendings'
      AND index_name = 'UX_auth_AccountMergePendings_MergeToken'
);

SET @ux_merge_token_sql := IF(
    @ux_merge_token_exists = 0,
    'CREATE UNIQUE INDEX `UX_auth_AccountMergePendings_MergeToken` ON `auth`.`AccountMergePendings` (`MergeToken`)',
    'SELECT 1');

PREPARE ux_merge_token_stmt FROM @ux_merge_token_sql;
EXECUTE ux_merge_token_stmt;
DEALLOCATE PREPARE ux_merge_token_stmt;

SET @journal_exists := (
    SELECT COUNT(1)
    FROM information_schema.tables
    WHERE table_schema = DATABASE()
      AND table_name = '__MigrationsHistory'
);

SET @journal_sql := IF(
    @journal_exists > 0,
    'INSERT INTO `__MigrationsHistory` (`ScriptName`, `Applied`)
     SELECT ''2_03_auth_AccountMergePendings.sql'', UTC_TIMESTAMP(6)
     FROM DUAL
     WHERE NOT EXISTS (
         SELECT 1 FROM `__MigrationsHistory`
         WHERE `ScriptName` = ''2_03_auth_AccountMergePendings.sql''
     )',
    'SELECT 1');

PREPARE journal_stmt FROM @journal_sql;
EXECUTE journal_stmt;
DEALLOCATE PREPARE journal_stmt;
