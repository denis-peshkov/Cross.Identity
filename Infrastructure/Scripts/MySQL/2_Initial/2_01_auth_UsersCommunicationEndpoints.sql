CREATE TABLE `auth`.`UsersCommunicationEndpoints`
(
    `UserCommunicationEndpointId` CHAR(36)     NOT NULL,
    `UserId`                      CHAR(36)     NOT NULL,
    `Channel`                     SMALLINT     NOT NULL,
    `Address`                     VARCHAR(320) NOT NULL,
    `IsVerified`                  TINYINT(1)   NOT NULL,
    `Source`                      SMALLINT     NOT NULL,
    `SourceRefId`                 BIGINT       NULL,
    `IsPreferred`                 TINYINT(1)   NOT NULL,
    `CreatedAt`                   DATETIME(6)  NOT NULL,
    `UpdatedAt`                   DATETIME(6)  NULL,
    `ConcurrencyStamp`            CHAR(36)     NOT NULL,

    CONSTRAINT `PK_auth_UsersCommunicationEndpoints` PRIMARY KEY (`UserCommunicationEndpointId`)
) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

CREATE UNIQUE INDEX `UX_auth_UsersCommunicationEndpoints_User_Channel_Address`
    ON `auth`.`UsersCommunicationEndpoints` (`UserId`, `Channel`, `Address`);
CREATE INDEX `IX_auth_UsersCommunicationEndpoints_UserId`
    ON `auth`.`UsersCommunicationEndpoints` (`UserId`);
-- MySQL 8.0.13+ functional/partial indexes are limited; preferred uniqueness enforced in app + generated column workaround:
-- unique on (UserId, preferred_key) where preferred_key is 1 when preferred else NULL is not portable.
-- Enforce single preferred in application code (CommunicationEndpointService.SetPreferredAsync).
