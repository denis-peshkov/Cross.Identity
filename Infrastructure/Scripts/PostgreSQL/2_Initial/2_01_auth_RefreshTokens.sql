CREATE TABLE auth."RefreshTokens"
(
    "RefreshTokenId"    uuid         NOT NULL,
    "FamilyId"          uuid         NOT NULL,
    "UserId"            uuid         NOT NULL,
    "TokenHash"         varchar(64)  NOT NULL,
    "ExpiresAt"         timestamp without time zone NOT NULL,
    "AbsoluteExpiresAt" timestamp without time zone NOT NULL,
    "CreatedAt"         timestamp without time zone NOT NULL,
    "ReplacedByTokenId" uuid         NULL,
    "RevokedAt"         timestamp without time zone NULL,
    "RevokeReason"      smallint     NULL,
    "RevokedByIp"       varchar(45)  NULL,
    "DeviceFingerprint" varchar(100) NULL,
    "UserAgent"         varchar(512) NULL,
    "IpAddress"         varchar(45)  NULL,
    "ConcurrencyStamp"  uuid         NOT NULL,

    CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("RefreshTokenId")
);

CREATE INDEX "IX_auth_RefreshTokens_User" ON auth."RefreshTokens" ("UserId");
CREATE INDEX "IX_auth_RefreshTokens_TokenHash" ON auth."RefreshTokens" ("TokenHash");
CREATE INDEX "IX_auth_RefreshTokens_Expires" ON auth."RefreshTokens" ("ExpiresAt");
CREATE INDEX "IX_auth_RefreshTokens_Revoked" ON auth."RefreshTokens" ("RevokedAt");
