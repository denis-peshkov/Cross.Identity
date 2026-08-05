CREATE TABLE `auth`.`Providers`
(
    `ProviderId`       SMALLINT     NOT NULL,
    `Name`             VARCHAR(50)  NOT NULL,
    `Scheme`           VARCHAR(100) NOT NULL,
    `IsEnabled`        TINYINT(1)   NOT NULL,
    `CreatedAt`        DATETIME(6)  NOT NULL,
    `ConcurrencyStamp` CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_Providers` PRIMARY KEY (`ProviderId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_Providers_Name`   ON `auth`.`Providers` (`Name`);
CREATE UNIQUE INDEX `UX_auth_Providers_Scheme` ON `auth`.`Providers` (`Scheme`);
