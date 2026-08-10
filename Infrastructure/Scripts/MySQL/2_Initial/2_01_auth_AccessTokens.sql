CREATE TABLE `auth`.`AccessTokens`
(
    `AccessTokenId`    CHAR(36)    NOT NULL,
    `FamilyId`         CHAR(36)    NOT NULL,
    `UserAccountId`    CHAR(36)    NOT NULL,
    `TokenHash`        VARCHAR(64) NOT NULL,
    `ExpiresAt`        DATETIME(6) NOT NULL,
    `CreatedAt`        DATETIME(6) NOT NULL,
    `RevokedAt`        DATETIME(6) NULL,
    `ConcurrencyStamp` CHAR(36)    NOT NULL,

    CONSTRAINT `PK_auth_AccessTokens` PRIMARY KEY (`AccessTokenId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_AccessTokens_UserAccountId` ON `auth`.`AccessTokens` (`UserAccountId`);
CREATE INDEX `IX_auth_AccessTokens_TokenHash` ON `auth`.`AccessTokens` (`TokenHash`);
