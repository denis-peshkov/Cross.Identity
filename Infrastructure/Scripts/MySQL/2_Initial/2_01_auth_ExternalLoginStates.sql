CREATE TABLE `auth`.`ExternalLoginStates`
(
    `ExternalLoginStateId` CHAR(36)     NOT NULL,
    `UserAccountId`        CHAR(36)     NULL,
    `Nonce`                VARCHAR(32)  NOT NULL,
    `Provider`             VARCHAR(64)  NOT NULL,
    `ReturnUrl`            VARCHAR(512) NULL,
    `ExpiresAt`            DATETIME(6)  NOT NULL,
    `CreatedAt`            DATETIME(6)  NOT NULL,
    `ConcurrencyStamp`     CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_ExternalLoginStates` PRIMARY KEY (`ExternalLoginStateId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_ExternalLoginStates_Nonce`
    ON `auth`.`ExternalLoginStates` (`Nonce`);
