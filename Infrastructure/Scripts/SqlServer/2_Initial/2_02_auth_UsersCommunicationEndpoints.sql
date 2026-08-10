ALTER TABLE [auth].[UsersCommunicationEndpoints]
    ADD CONSTRAINT [FK_auth_UsersCommunicationEndpoints_UserAccount]
        FOREIGN KEY ([UserAccountId])
            REFERENCES [auth].[UsersAccounts] ([UserAccountId])
            ON DELETE CASCADE;
GO
