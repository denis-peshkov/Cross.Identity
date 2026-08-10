CREATE TABLE auth."Audits"
(
    "AuditId"           uuid          NOT NULL,
    "UserAccountId"     uuid          NOT NULL,
    "Operation"         smallint      NOT NULL,
    "EntityType"        smallint      NOT NULL,
    "EntityId"          varchar(64)   NULL,
    "RevokedReason"     smallint      NULL,
    "IpAddress"         varchar(64)   NULL,
    "UserAgent"         varchar(512)  NULL,
    "DeviceFingerprint" varchar(128)  NULL,
    "Notes"             varchar(2000) NULL,
    "CreatedAt"         timestamp without time zone NOT NULL,

    CONSTRAINT "PK_auth_Audits" PRIMARY KEY ("AuditId")
);

CREATE INDEX "IX_auth_Audits_CreatedAt" ON auth."Audits" ("CreatedAt");
CREATE INDEX "IX_auth_Audits_UserAccountId" ON auth."Audits" ("UserAccountId");
CREATE INDEX "IX_auth_Audits_Operation" ON auth."Audits" ("Operation");
CREATE INDEX "IX_auth_Audits_Entity" ON auth."Audits" ("EntityType", "EntityId");
