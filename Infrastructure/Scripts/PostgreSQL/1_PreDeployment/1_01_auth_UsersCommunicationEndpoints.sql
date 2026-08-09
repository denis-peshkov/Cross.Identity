CREATE TABLE auth."UsersCommunicationEndpoints"
(
    "UserCommunicationEndpointId" uuid         NOT NULL,
    "UserId"                      uuid         NOT NULL,
    "Channel"                     smallint     NOT NULL,
    "Address"                     varchar(320) NOT NULL,
    "IsVerified"                  boolean      NOT NULL,
    "Source"                      smallint     NOT NULL,
    "SourceRefId"                 bigint       NULL,
    "IsPreferred"                 boolean      NOT NULL,
    "CreatedAt"                   timestamp without time zone NOT NULL,
    "UpdatedAt"                   timestamp without time zone NULL,
    "ConcurrencyStamp"            uuid         NOT NULL,

    CONSTRAINT "PK_auth_UsersCommunicationEndpoints" PRIMARY KEY ("UserCommunicationEndpointId")
);

CREATE UNIQUE INDEX "UX_auth_UsersCommunicationEndpoints_User_Channel_Address"
    ON auth."UsersCommunicationEndpoints" ("UserId", "Channel", "Address");
CREATE INDEX "IX_auth_UsersCommunicationEndpoints_UserId"
    ON auth."UsersCommunicationEndpoints" ("UserId");
CREATE UNIQUE INDEX "UX_auth_UsersCommunicationEndpoints_User_Preferred"
    ON auth."UsersCommunicationEndpoints" ("UserId")
    WHERE "IsPreferred" = true;

ALTER TABLE auth."UsersCommunicationEndpoints"
    ADD CONSTRAINT "FK_auth_UsersCommunicationEndpoints_User"
        FOREIGN KEY ("UserId")
            REFERENCES auth."UsersAccounts" ("UserAccountId")
            ON DELETE CASCADE;
