CREATE TABLE `auth`.`PhoneVerifications`
(
    `PhoneVerificationId` BIGINT       NOT NULL AUTO_INCREMENT,
    `UserAccountId`       CHAR(36)     NOT NULL,
    `PhoneNumber`         VARCHAR(20)  NOT NULL,
    `CodeHash`            BINARY(32)   NOT NULL,
    `CodeLength`          TINYINT      NOT NULL,
    `Attempts`            TINYINT      NOT NULL,
    `MaxAttempts`         TINYINT      NOT NULL,
    `ExpiresAt`           DATETIME(6)  NOT NULL,
    `UsedAt`              DATETIME(6)  NULL,
    `CreatedAt`           DATETIME(6)  NOT NULL,
    `ConcurrencyStamp`    CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_PhoneVerifications` PRIMARY KEY (`PhoneVerificationId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_PhoneVerifications_UserAccount` ON `auth`.`PhoneVerifications` (`UserAccountId`);
CREATE INDEX `IX_auth_PhoneVerifications_CodeHash` ON `auth`.`PhoneVerifications` (`CodeHash`);
CREATE INDEX `IX_auth_PhoneVerifications_ExpiresAt` ON `auth`.`PhoneVerifications` (`ExpiresAt`);
CREATE INDEX `IX_auth_PhoneVerifications_Phone` ON `auth`.`PhoneVerifications` (`PhoneNumber`);
