CREATE TABLE `auth`.`EmailVerifications`
(
    `EmailVerificationId` CHAR(36)     NOT NULL,
    `UserAccountId`       CHAR(36)     NOT NULL,
    `Email`               VARCHAR(320) NOT NULL,
    `TokenHash`           BINARY(32)   NOT NULL,
    `TokenLength`         TINYINT      NOT NULL,
    `Attempts`            TINYINT      NOT NULL,
    `MaxAttempts`         TINYINT      NOT NULL,
    `ExpiresAt`           DATETIME(6)  NOT NULL,
    `UsedAt`              DATETIME(6)  NULL,
    `CreatedAt`           DATETIME(6)  NOT NULL,
    `ConcurrencyStamp`    CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_EmailVerifications` PRIMARY KEY (`EmailVerificationId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_EmailVerifications_UserAccount` ON `auth`.`EmailVerifications` (`UserAccountId`);
CREATE INDEX `IX_auth_EmailVerifications_Email` ON `auth`.`EmailVerifications` (`Email`);
CREATE INDEX `IX_auth_EmailVerifications_TokenHash` ON `auth`.`EmailVerifications` (`TokenHash`);
CREATE INDEX `IX_auth_EmailVerifications_ExpiresAt` ON `auth`.`EmailVerifications` (`ExpiresAt`);
