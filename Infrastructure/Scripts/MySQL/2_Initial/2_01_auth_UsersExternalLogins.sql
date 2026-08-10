CREATE TABLE `auth`.`UsersExternalLogins`
(
    `UserExternalLoginId` CHAR(36)     NOT NULL,
    `UserAccountId`       CHAR(36)     NOT NULL,
    `ProviderId`          SMALLINT     NOT NULL,
    `ProviderUserId`      VARCHAR(200) NOT NULL,
    `ProviderEmail`       VARCHAR(200) NULL,
    `DisplayName`         VARCHAR(200) NULL,
    `AvatarUrl`           VARCHAR(500) NULL,
    `ProfileUrl`          VARCHAR(500) NULL,
    `AccessTokenEnc`      LONGBLOB     NULL,
    `RefreshTokenEnc`     LONGBLOB     NULL,
    `ExpiresAt`           DATETIME(6)  NULL,
    `Scope`               VARCHAR(500) NULL,
    `CreatedAt`           DATETIME(6)  NOT NULL,
    `UpdatedAt`           DATETIME(6)  NULL,
    `ConcurrencyStamp`    CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_UsersExternalLogins` PRIMARY KEY (`UserExternalLoginId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_UsersExternalLogins_Provider_User`
    ON `auth`.`UsersExternalLogins` (`ProviderId`, `ProviderUserId`);
CREATE INDEX `IX_auth_UsersExternalLogins_UserAccountId`
    ON `auth`.`UsersExternalLogins` (`UserAccountId`);
CREATE INDEX `IX_auth_UsersExternalLogins_ProviderUserId`
    ON `auth`.`UsersExternalLogins` (`ProviderUserId`);
