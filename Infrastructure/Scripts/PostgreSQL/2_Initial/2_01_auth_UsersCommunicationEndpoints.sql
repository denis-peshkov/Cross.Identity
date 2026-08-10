CREATE TABLE auth."UsersCommunicationEndpoints"
(
    "UserCommunicationEndpointId" uuid         NOT NULL,
    "UserAccountId"               uuid         NOT NULL,
    "Channel"                     smallint     NOT NULL,
    "Address"                     varchar(320) NOT NULL,
    "IsVerified"                  boolean      NOT NULL,
    "Source"                      smallint     NOT NULL,
    "EntityId"                    uuid         NULL,
    "IsPreferred"                 boolean      NOT NULL,
    "CreatedAt"                   timestamp without time zone NOT NULL,
    "UpdatedAt"                   timestamp without time zone NULL,
    "ConcurrencyStamp"            uuid         NOT NULL,

    CONSTRAINT "PK_auth_UsersCommunicationEndpoints" PRIMARY KEY ("UserCommunicationEndpointId")
);

CREATE UNIQUE INDEX "UX_auth_UsersCommunicationEndpoints_User_Channel_Address"
    ON auth."UsersCommunicationEndpoints" ("UserAccountId", "Channel", "Address");
CREATE INDEX "IX_auth_UsersCommunicationEndpoints_UserAccountId"
    ON auth."UsersCommunicationEndpoints" ("UserAccountId");
CREATE INDEX "IX_auth_UsersCommunicationEndpoints_EntityId"
    ON auth."UsersCommunicationEndpoints" ("EntityId");
