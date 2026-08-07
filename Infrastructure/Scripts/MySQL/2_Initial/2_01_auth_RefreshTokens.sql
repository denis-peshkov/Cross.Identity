CREATE TABLE `auth`.`RefreshTokens`
(
    `RefreshTokenId`    CHAR(36)     NOT NULL,
    `FamilyId`          CHAR(36)     NOT NULL,
    `UserId`            CHAR(36)     NOT NULL,
    `TokenHash`         VARCHAR(64)  NOT NULL,
    `ExpiresAt`         DATETIME(6)  NOT NULL,
    `AbsoluteExpiresAt` DATETIME(6)  NOT NULL,
    `CreatedAt`         DATETIME(6)  NOT NULL,
    `ReplacedByTokenId` CHAR(36)     NULL,
    `RevokedAt`         DATETIME(6)  NULL,
    `RevokedReason`      SMALLINT     NULL,
    `RevokedIpAddress`  VARCHAR(45)  NULL,
    `RevokedUserAgent`  VARCHAR(512) NULL,
    `CreatedDeviceFingerprint` VARCHAR(100) NULL,
    `CreatedUserAgent`         VARCHAR(512) NULL,
    `CreatedIpAddress`         VARCHAR(45)  NULL,
    `ConcurrencyStamp`  CHAR(36)     NOT NULL,

    CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`RefreshTokenId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_RefreshTokens_User` ON `auth`.`RefreshTokens` (`UserId`);
CREATE INDEX `IX_auth_RefreshTokens_TokenHash` ON `auth`.`RefreshTokens` (`TokenHash`);
CREATE INDEX `IX_auth_RefreshTokens_Expires` ON `auth`.`RefreshTokens` (`ExpiresAt`);
CREATE INDEX `IX_auth_RefreshTokens_Revoked` ON `auth`.`RefreshTokens` (`RevokedAt`);
