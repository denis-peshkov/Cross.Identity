ALTER TABLE [auth].[UsersCommunicationEndpoints]
    ADD CONSTRAINT [FK_auth_UsersCommunicationEndpoints_User]
        FOREIGN KEY ([UserId])
            REFERENCES [auth].[UsersAccounts] ([UserAccountId])
            ON DELETE CASCADE;
GO
