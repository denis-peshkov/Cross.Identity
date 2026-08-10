ALTER TABLE auth."UsersExternalLogins"
    ADD CONSTRAINT "FK_auth_UsersExternalLogins_UserAccount"
        FOREIGN KEY ("UserAccountId")
            REFERENCES auth."UsersAccounts" ("UserAccountId")
            ON DELETE CASCADE;

ALTER TABLE auth."UsersExternalLogins"
    ADD CONSTRAINT "FK_auth_UsersExternalLogins_Provider"
        FOREIGN KEY ("ProviderId")
            REFERENCES auth."Providers" ("ProviderId")
            ON DELETE NO ACTION;
