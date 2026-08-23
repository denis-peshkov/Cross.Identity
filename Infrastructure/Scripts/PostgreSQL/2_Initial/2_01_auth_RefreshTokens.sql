CREATE TABLE auth."RefreshTokens"
(
    "RefreshTokenId"           uuid                           NOT NULL,
    "FamilyId"                 uuid                           NOT NULL,
    "UserAccountId"            uuid                           NOT NULL,
    "TokenHash"                varchar(64)                    NOT NULL,
    "ExpiresAt"                timestamp without time zone    NOT NULL,
    "AbsoluteExpiresAt"        timestamp without time zone    NOT NULL,
    "CreatedAt"                timestamp without time zone    NOT NULL,
    "CreatedIpAddress"         varchar(64)                    NULL,
    "CreatedUserAgent"         varchar(512)                   NULL,
    "CreatedDeviceFingerprint" varchar(128)                   NULL,
    "LastActivityAt"           timestamp without time zone    NOT NULL,
    "ReplacedByTokenId"        uuid                           NULL,
    "RevokedAt"                timestamp without time zone    NULL,
    "ConcurrencyStamp"         uuid                           NOT NULL,

    CONSTRAINT "PK_auth_RefreshTokens" PRIMARY KEY ("RefreshTokenId")
);

CREATE INDEX "IX_auth_RefreshTokens_UserAccountId" ON auth."RefreshTokens" ("UserAccountId");
CREATE INDEX "IX_auth_RefreshTokens_TokenHash" ON auth."RefreshTokens" ("TokenHash");
