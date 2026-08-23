DROP INDEX IF EXISTS [UX_auth_UsersAccounts_Email] ON [auth].[UsersAccounts];
GO

CREATE UNIQUE INDEX [UX_auth_UsersAccounts_Email]
    ON [auth].[UsersAccounts]([Email])
    WHERE [Email] IS NOT NULL AND [EmailConfirmed] = 1;
GO
