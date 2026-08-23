ALTER TABLE auth."AccessTokens"
    ADD CONSTRAINT "FK_auth_AccessTokens_UserAccount"
        FOREIGN KEY ("UserAccountId")
            REFERENCES auth."UsersAccounts" ("UserAccountId")
            ON DELETE CASCADE;
