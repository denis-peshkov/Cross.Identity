CREATE TABLE auth."AccessTokens"
(
    "AccessTokenId"    uuid        NOT NULL,
    "FamilyId"         uuid        NOT NULL,
    "UserAccountId"    uuid        NOT NULL,
    "TokenHash"        varchar(64) NOT NULL,
    "ExpiresAt"        timestamp without time zone NOT NULL,
    "CreatedAt"        timestamp without time zone NOT NULL,
    "RevokedAt"        timestamp without time zone NULL,
    "ConcurrencyStamp" uuid        NOT NULL,

    CONSTRAINT "PK_auth_AccessTokens" PRIMARY KEY ("AccessTokenId")
);

CREATE INDEX "IX_auth_AccessTokens_UserAccountId" ON auth."AccessTokens" ("UserAccountId");
CREATE INDEX "IX_auth_AccessTokens_TokenHash" ON auth."AccessTokens" ("TokenHash");
