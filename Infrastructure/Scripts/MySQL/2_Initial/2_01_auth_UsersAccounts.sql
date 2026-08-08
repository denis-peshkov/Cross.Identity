CREATE TABLE `auth`.`UsersAccounts`
(
    `UserAccountId`         CHAR(36)     NOT NULL,
    `UserName`              VARCHAR(200) NULL,
    `NormalizedUserName`    VARCHAR(200) NULL,
    `Email`                 VARCHAR(200) NULL,
    `PhoneNumber`           VARCHAR(20)  NULL,
    `PasswordPhc`           VARCHAR(800) NULL,
    `PasswordHash`          BINARY(32)   NULL,
    `PasswordSalt`          VARCHAR(200) NULL,
    `PasswordPepperVersion` SMALLINT     NOT NULL,

    `LockoutEnd`            DATETIME(6)  NULL,
    `LockoutEnabled`        TINYINT(1)   NOT NULL,
    `AccessFailedCount`     INT          NOT NULL,

    `SecurityStamp`         CHAR(36)     NULL,
    `ConcurrencyStamp`      CHAR(36)     NOT NULL,

    `EmailConfirmed`        TINYINT(1)   NOT NULL,
    `PhoneNumberConfirmed`  TINYINT(1)   NOT NULL,
    `TwoFactorEnabled`      TINYINT(1)   NOT NULL,

    `IsActive`              TINYINT(1)   NOT NULL,

    `CreatedAt`             DATETIME(6)  NOT NULL,
    `CreatedBy`             CHAR(36)     NOT NULL,
    `LastLoginAt`           DATETIME(6)  NULL,

    CONSTRAINT `PK_auth_UsersAccounts` PRIMARY KEY (`UserAccountId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Multiple NULLs allowed under UNIQUE (same effect as SQL Server filtered unique indexes).
CREATE UNIQUE INDEX `UX_auth_UsersAccounts_UserName`
    ON `auth`.`UsersAccounts` (`NormalizedUserName`);

CREATE UNIQUE INDEX `UX_auth_UsersAccounts_Email`
    ON `auth`.`UsersAccounts` (`Email`);

CREATE UNIQUE INDEX `UX_auth_UsersAccounts_Phone`
    ON `auth`.`UsersAccounts` (`PhoneNumber`);
