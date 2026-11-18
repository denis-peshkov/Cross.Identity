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

CREATE UNIQUE INDEX `UX_auth_AccountMergePendings_MergeToken`
    ON `auth`.`AccountMergePendings` (`MergeToken`);
