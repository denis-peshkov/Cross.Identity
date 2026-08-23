DROP INDEX IF EXISTS [UX_auth_UsersAccounts_Phone] ON [auth].[UsersAccounts];
GO

CREATE UNIQUE INDEX [UX_auth_UsersAccounts_Phone]
    ON [auth].[UsersAccounts]([PhoneNumber])
    WHERE [PhoneNumber] IS NOT NULL AND [PhoneNumberConfirmed] = 1;
GO
