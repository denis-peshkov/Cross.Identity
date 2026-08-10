ALTER TABLE [auth].[Audits]
    ADD CONSTRAINT [FK_auth_Audits_UserAccount]
        FOREIGN KEY ([UserAccountId])
            REFERENCES [auth].[UsersAccounts] ([UserAccountId])
            ON DELETE CASCADE;
GO
