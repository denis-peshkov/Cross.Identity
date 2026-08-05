ALTER TABLE auth."RefreshTokens"
    ADD CONSTRAINT "FK_auth_RefreshTokens_User"
        FOREIGN KEY ("UserId")
            REFERENCES auth."UsersAccounts" ("UserAccountId")
            ON DELETE CASCADE;
