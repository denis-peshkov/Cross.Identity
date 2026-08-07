CREATE TABLE `auth`.`AccessTokens`
(
    `AccessTokenId`     CHAR(36)     NOT NULL,
    `FamilyId`          CHAR(36)     NOT NULL,
    `UserId`            CHAR(36)     NOT NULL,
    `TokenHash`         VARCHAR(64)  NOT NULL,
    `ExpiresAt`         DATETIME(6)  NOT NULL,
    `CreatedAt`         DATETIME(6)  NOT NULL,
    `RevokedAt`         DATETIME(6)  NULL,
    `RevokedReason`      SMALLINT     NULL,
    `RevokedIpAddress`  VARCHAR(45)  NULL,
    `RevokedUserAgent`  VARCHAR(512) NULL,
    `CreatedDeviceFingerprint` VARCHAR(100) NULL,
    `CreatedUserAgent`         VARCHAR(512) NULL,
    `CreatedIpAddress`         VARCHAR(45)  NULL,
    `ConcurrencyStamp`  CHAR(36)     NOT NULL,

    CONSTRAINT `PK_AccessTokens` PRIMARY KEY (`AccessTokenId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_AccessTokens_User` ON `auth`.`AccessTokens` (`UserId`);
CREATE INDEX `IX_auth_AccessTokens_TokenHash` ON `auth`.`AccessTokens` (`TokenHash`);
CREATE INDEX `IX_auth_AccessTokens_Expires` ON `auth`.`AccessTokens` (`ExpiresAt`);
CREATE INDEX `IX_auth_AccessTokens_Revoked` ON `auth`.`AccessTokens` (`RevokedAt`);
