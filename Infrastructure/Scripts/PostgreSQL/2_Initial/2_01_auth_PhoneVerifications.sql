CREATE TABLE auth."PhoneVerifications"
(
    "PhoneVerificationId" uuid        NOT NULL,
    "UserAccountId"       uuid        NOT NULL,
    "PhoneNumber"         varchar(20) NOT NULL,
    "CodeHash"            bytea       NOT NULL,
    "CodeLength"          smallint    NOT NULL,
    "Attempts"            smallint    NOT NULL,
    "MaxAttempts"         smallint    NOT NULL,
    "ExpiresAt"           timestamp without time zone NOT NULL,
    "UsedAt"              timestamp without time zone NULL,
    "CreatedAt"           timestamp without time zone NOT NULL,
    "ConcurrencyStamp"    uuid        NOT NULL,

    CONSTRAINT "PK_auth_PhoneVerifications" PRIMARY KEY ("PhoneVerificationId")
);

CREATE INDEX "IX_auth_PhoneVerifications_UserAccount" ON auth."PhoneVerifications" ("UserAccountId");
CREATE INDEX "IX_auth_PhoneVerifications_CodeHash" ON auth."PhoneVerifications" ("CodeHash");
CREATE INDEX "IX_auth_PhoneVerifications_ExpiresAt" ON auth."PhoneVerifications" ("ExpiresAt");
