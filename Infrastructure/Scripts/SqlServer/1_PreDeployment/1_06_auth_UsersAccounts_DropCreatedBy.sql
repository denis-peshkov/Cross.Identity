IF COL_LENGTH(N'auth.UsersAccounts', N'CreatedBy') IS NOT NULL
BEGIN
    ALTER TABLE [auth].[UsersAccounts] DROP COLUMN [CreatedBy];
END
GO
