DROP INDEX IF EXISTS auth."UX_auth_UsersAccounts_Email";

CREATE UNIQUE INDEX "UX_auth_UsersAccounts_Email"
    ON auth."UsersAccounts" ("Email")
    WHERE "Email" IS NOT NULL AND "EmailVerified" = true;
