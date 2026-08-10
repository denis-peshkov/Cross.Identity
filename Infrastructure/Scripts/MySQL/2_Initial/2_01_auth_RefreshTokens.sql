CREATE TABLE `auth`.`RefreshTokens`
(
    `RefreshTokenId`    CHAR(36)    NOT NULL,
    `FamilyId`          CHAR(36)    NOT NULL,
    `UserAccountId`     CHAR(36)    NOT NULL,
    `TokenHash`         VARCHAR(64) NOT NULL,
    `ExpiresAt`         DATETIME(6) NOT NULL,
    `AbsoluteExpiresAt` DATETIME(6) NOT NULL,
    `CreatedAt`         DATETIME(6) NOT NULL,
    `ReplacedByTokenId` CHAR(36)    NULL,
    `RevokedAt`         DATETIME(6) NULL,
    `ConcurrencyStamp`  CHAR(36)    NOT NULL,

    CONSTRAINT `PK_auth_RefreshTokens` PRIMARY KEY (`RefreshTokenId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_RefreshTokens_UserAccountId` ON `auth`.`RefreshTokens` (`UserAccountId`);
CREATE INDEX `IX_auth_RefreshTokens_TokenHash` ON `auth`.`RefreshTokens` (`TokenHash`);
