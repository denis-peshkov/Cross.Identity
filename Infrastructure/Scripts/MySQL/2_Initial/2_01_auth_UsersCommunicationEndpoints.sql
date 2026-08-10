CREATE TABLE `auth`.`UsersCommunicationEndpoints`
(
    `UserCommunicationEndpointId` CHAR(36)     NOT NULL,
    `UserAccountId`               CHAR(36)     NOT NULL,
    `Channel`                     SMALLINT     NOT NULL,
    `Address`                     VARCHAR(320) NOT NULL,
    `IsVerified`                  TINYINT(1)   NOT NULL,
    `Source`                      SMALLINT     NOT NULL,
    `EntityId`                    CHAR(36)     NULL,
    `IsPreferred`                 TINYINT(1)   NOT NULL,
    `CreatedAt`                   DATETIME(6)  NOT NULL,
    `UpdatedAt`                   DATETIME(6)  NULL,
    `ConcurrencyStamp`            CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_UsersCommunicationEndpoints` PRIMARY KEY (`UserCommunicationEndpointId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_UsersCommunicationEndpoints_User_Channel_Address`
    ON `auth`.`UsersCommunicationEndpoints` (`UserAccountId`, `Channel`, `Address`);
CREATE INDEX `IX_auth_UsersCommunicationEndpoints_UserAccountId`
    ON `auth`.`UsersCommunicationEndpoints` (`UserAccountId`);
CREATE INDEX `IX_auth_UsersCommunicationEndpoints_EntityId`
    ON `auth`.`UsersCommunicationEndpoints` (`EntityId`);
