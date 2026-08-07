CREATE TABLE auth."AccessTokens"
(
    "AccessTokenId"     uuid         NOT NULL,
    "FamilyId"          uuid         NOT NULL,
    "UserId"            uuid         NOT NULL,
    "TokenHash"         varchar(64)  NOT NULL,
    "ExpiresAt"         timestamp without time zone NOT NULL,
    "CreatedAt"         timestamp without time zone NOT NULL,
    "RevokedAt"         timestamp without time zone NULL,
    "RevokedReason"      smallint     NULL,
    "RevokedIpAddress"  varchar(45)  NULL,
    "RevokedUserAgent"  varchar(512) NULL,
    "CreatedDeviceFingerprint" varchar(100) NULL,
    "CreatedUserAgent"         varchar(512) NULL,
    "CreatedIpAddress"         varchar(45)  NULL,
    "ConcurrencyStamp"  uuid         NOT NULL,

    CONSTRAINT "PK_AccessTokens" PRIMARY KEY ("AccessTokenId")
);

CREATE INDEX "IX_auth_AccessTokens_User" ON auth."AccessTokens" ("UserId");
CREATE INDEX "IX_auth_AccessTokens_TokenHash" ON auth."AccessTokens" ("TokenHash");
CREATE INDEX "IX_auth_AccessTokens_Expires" ON auth."AccessTokens" ("ExpiresAt");
CREATE INDEX "IX_auth_AccessTokens_Revoked" ON auth."AccessTokens" ("RevokedAt");
