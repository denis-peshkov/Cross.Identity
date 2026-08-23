CREATE TABLE `auth`.`Audits`
(
    `AuditId`           CHAR(36)      NOT NULL,
    `UserAccountId`     CHAR(36)      NOT NULL,
    `Operation`         SMALLINT      NOT NULL,
    `EntityType`        SMALLINT      NOT NULL,
    `EntityId`          VARCHAR(64)   NULL,
    `RevokedReason`     SMALLINT      NULL,
    `IpAddress`         VARCHAR(64)   NULL,
    `UserAgent`         VARCHAR(512)  NULL,
    `DeviceFingerprint` VARCHAR(128)  NULL,
    `Notes`             VARCHAR(2000) NULL,
    `CreatedAt`         DATETIME(6)   NOT NULL,

    CONSTRAINT `PK_auth_Audits` PRIMARY KEY (`AuditId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE INDEX `IX_auth_Audits_CreatedAt` ON `auth`.`Audits` (`CreatedAt`);
CREATE INDEX `IX_auth_Audits_UserAccountId` ON `auth`.`Audits` (`UserAccountId`);
CREATE INDEX `IX_auth_Audits_Operation` ON `auth`.`Audits` (`Operation`);
CREATE INDEX `IX_auth_Audits_Entity` ON `auth`.`Audits` (`EntityType`, `EntityId`);
