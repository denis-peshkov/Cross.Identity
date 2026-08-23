CREATE TABLE auth."EmailVerifications"
(
    "EmailVerificationId" uuid         NOT NULL,
    "UserAccountId"       uuid         NOT NULL,
    "Email"               varchar(320) NOT NULL,
    "TokenHash"           bytea        NOT NULL,
    "TokenLength"         smallint     NOT NULL,
    "Attempts"            smallint     NOT NULL,
    "MaxAttempts"         smallint     NOT NULL,
    "ExpiresAt"           timestamp without time zone NOT NULL,
    "UsedAt"              timestamp without time zone NULL,
    "CreatedAt"           timestamp without time zone NOT NULL,
    "ConcurrencyStamp"    uuid         NOT NULL,

    CONSTRAINT "PK_auth_EmailVerifications" PRIMARY KEY ("EmailVerificationId")
);

CREATE INDEX "IX_auth_EmailVerifications_UserAccount" ON auth."EmailVerifications" ("UserAccountId");
CREATE INDEX "IX_auth_EmailVerifications_Email" ON auth."EmailVerifications" ("Email");
CREATE INDEX "IX_auth_EmailVerifications_TokenHash" ON auth."EmailVerifications" ("TokenHash");
CREATE INDEX "IX_auth_EmailVerifications_ExpiresAt" ON auth."EmailVerifications" ("ExpiresAt");
