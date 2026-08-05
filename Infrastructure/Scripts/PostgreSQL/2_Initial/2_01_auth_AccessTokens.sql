CREATE TABLE auth."AccessTokens"
(
    "AccessTokenId"     uuid         NOT NULL,
    "FamilyId"          uuid         NOT NULL,
    "UserId"            uuid         NOT NULL,
    "TokenHash"         varchar(64)  NOT NULL,
    "ExpiresAt"         timestamp without time zone NOT NULL,
    "CreatedAt"         timestamp without time zone NOT NULL,
    "RevokedAt"         timestamp without time zone NULL,
    "RevokeReason"      smallint     NULL,
    "RevokedByIp"       varchar(45)  NULL,
    "DeviceFingerprint" varchar(100) NULL,
    "UserAgent"         varchar(512) NULL,
    "IpAddress"         varchar(45)  NULL,
    "ConcurrencyStamp"  uuid         NOT NULL,

    CONSTRAINT "PK_AccessTokens" PRIMARY KEY ("AccessTokenId")
);

CREATE INDEX "IX_auth_AccessTokens_User" ON auth."AccessTokens" ("UserId");
CREATE INDEX "IX_auth_AccessTokens_TokenHash" ON auth."AccessTokens" ("TokenHash");
CREATE INDEX "IX_auth_AccessTokens_Expires" ON auth."AccessTokens" ("ExpiresAt");
CREATE INDEX "IX_auth_AccessTokens_Revoked" ON auth."AccessTokens" ("RevokedAt");
