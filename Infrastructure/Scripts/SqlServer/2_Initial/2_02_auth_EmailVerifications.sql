ALTER TABLE [auth].[EmailVerifications]
    ADD CONSTRAINT [FK_auth_EmailVerifications_UserAccount]
        FOREIGN KEY ([UserAccountId])
            REFERENCES [auth].[UsersAccounts] ([UserAccountId])
            ON DELETE CASCADE;
GO
