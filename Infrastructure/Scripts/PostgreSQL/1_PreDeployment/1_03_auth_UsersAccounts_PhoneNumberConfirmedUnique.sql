DROP INDEX IF EXISTS auth."UX_auth_UsersAccounts_Phone";

CREATE UNIQUE INDEX "UX_auth_UsersAccounts_Phone"
    ON auth."UsersAccounts" ("PhoneNumber")
    WHERE "PhoneNumber" IS NOT NULL AND "PhoneNumberConfirmed" = true;
