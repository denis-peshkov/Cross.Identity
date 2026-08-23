CREATE TABLE auth."ExternalLoginStates"
(
    "ExternalLoginStateId" uuid         NOT NULL,
    "UserAccountId"        uuid         NULL,
    "Nonce"                varchar(32)  NOT NULL,
    "Provider"             varchar(64)  NOT NULL,
    "ReturnUrl"            varchar(512) NULL,
    "ExpiresAt"            timestamp without time zone NOT NULL,
    "CreatedAt"            timestamp without time zone NOT NULL,
    "ConcurrencyStamp"     uuid         NOT NULL,

    CONSTRAINT "PK_auth_ExternalLoginStates" PRIMARY KEY ("ExternalLoginStateId")
);

CREATE UNIQUE INDEX "UX_auth_ExternalLoginStates_Nonce"
    ON auth."ExternalLoginStates" ("Nonce");
