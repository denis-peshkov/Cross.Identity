CREATE TABLE auth."UsersExternalLogins"
(
    "UserExternalLoginId" uuid         NOT NULL,
    "UserAccountId"       uuid         NOT NULL,
    "ProviderId"          smallint     NOT NULL,
    "ProviderUserId"      varchar(200) NOT NULL,
    "ProviderEmail"       varchar(200) NULL,
    "DisplayName"         varchar(200) NULL,
    "AvatarUrl"           varchar(500) NULL,
    "ProfileUrl"          varchar(500) NULL,
    "AccessTokenEnc"      bytea        NULL,
    "RefreshTokenEnc"     bytea        NULL,
    "ExpiresAt"           timestamp without time zone NULL,
    "Scope"               varchar(500) NULL,
    "CreatedAt"           timestamp without time zone NOT NULL,
    "UpdatedAt"           timestamp without time zone NULL,
    "ConcurrencyStamp"    uuid         NOT NULL,

    CONSTRAINT "PK_auth_UsersExternalLogins" PRIMARY KEY ("UserExternalLoginId")
);

CREATE UNIQUE INDEX "UX_auth_UsersExternalLogins_Provider_User"
    ON auth."UsersExternalLogins" ("ProviderId", "ProviderUserId");
CREATE INDEX "IX_auth_UsersExternalLogins_UserAccountId"
    ON auth."UsersExternalLogins" ("UserAccountId");
CREATE INDEX "IX_auth_UsersExternalLogins_ProviderUserId"
    ON auth."UsersExternalLogins" ("ProviderUserId");
