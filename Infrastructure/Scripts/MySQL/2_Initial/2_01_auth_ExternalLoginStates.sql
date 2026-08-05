CREATE TABLE `auth`.`ExternalLoginStates`
(
    `ExternalLoginStateId` BIGINT       NOT NULL AUTO_INCREMENT,
    `Nonce`                VARCHAR(32)  NOT NULL,
    `Provider`             VARCHAR(64)  NOT NULL,
    `ReturnUrl`            VARCHAR(512) NULL,
    `LinkUserId`           CHAR(36)     NULL,
    `ExpiresAt`            DATETIME(6)  NOT NULL,
    `CreatedAt`            DATETIME(6)  NOT NULL,
    `ConcurrencyStamp`     CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_ExternalLoginStates` PRIMARY KEY (`ExternalLoginStateId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_ExternalLoginStates_Nonce`
    ON `auth`.`ExternalLoginStates` (`Nonce`);
CREATE INDEX `IX_auth_ExternalLoginStates_ExpiresAt`
    ON `auth`.`ExternalLoginStates` (`ExpiresAt`);
