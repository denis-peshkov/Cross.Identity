ALTER TABLE [auth].[ExternalLoginStates]
    ADD CONSTRAINT [FK_auth_ExternalLoginStates_UserAccount]
        FOREIGN KEY ([UserAccountId])
            REFERENCES [auth].[UsersAccounts] ([UserAccountId])
            ON DELETE SET NULL;
GO
