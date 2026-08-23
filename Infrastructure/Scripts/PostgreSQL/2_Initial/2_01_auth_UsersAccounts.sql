CREATE TABLE auth."UsersAccounts"
(
    "UserAccountId"         uuid         NOT NULL,
    "UserName"              varchar(200) NULL,
    "NormalizedUserName"    varchar(200) NULL,
    "Email"                 varchar(200) NULL,
    "PhoneNumber"           varchar(20)  NULL,
    "PasswordPhc"           varchar(800) NULL,
    "PasswordHash"          bytea        NULL,
    "PasswordSalt"          varchar(200) NULL,
    "PasswordPepperVersion" smallint     NOT NULL,

    "LockoutEnd"            timestamptz  NULL,
    "LockoutEnabled"        boolean      NOT NULL,
    "AccessFailedCount"     integer      NOT NULL,

    "SecurityStamp"         uuid         NULL,
    "ConcurrencyStamp"      uuid         NOT NULL,

    "EmailConfirmed"        boolean      NOT NULL,
    "PhoneNumberConfirmed"  boolean      NOT NULL,
    "TwoFactorEnabled"      boolean      NOT NULL,

    "IsActive"              boolean      NOT NULL,

    "CreatedAt"             timestamp without time zone NOT NULL,
    "LastLoginAt"           timestamp without time zone NULL,

    CONSTRAINT "PK_auth_UsersAccounts" PRIMARY KEY ("UserAccountId")
);

-- Multiple NULLs allowed under UNIQUE (same effect as SQL Server filtered unique indexes).
CREATE UNIQUE INDEX "UX_auth_UsersAccounts_UserName"
    ON auth."UsersAccounts" ("NormalizedUserName");

CREATE UNIQUE INDEX "UX_auth_UsersAccounts_Email"
    ON auth."UsersAccounts" ("Email")
    WHERE "Email" IS NOT NULL AND "EmailConfirmed" = true;

CREATE UNIQUE INDEX "UX_auth_UsersAccounts_Phone"
    ON auth."UsersAccounts" ("PhoneNumber")
    WHERE "PhoneNumber" IS NOT NULL AND "PhoneNumberConfirmed" = true;
