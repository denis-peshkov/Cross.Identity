IF COL_LENGTH(N'auth.UsersAccounts', N'EmailConfirmed') IS NOT NULL
   AND COL_LENGTH(N'auth.UsersAccounts', N'EmailVerified') IS NULL
BEGIN
    EXEC sp_rename N'auth.UsersAccounts.EmailConfirmed', N'EmailVerified', N'COLUMN';
END
GO

IF COL_LENGTH(N'auth.UsersAccounts', N'PhoneNumberConfirmed') IS NOT NULL
   AND COL_LENGTH(N'auth.UsersAccounts', N'PhoneNumberVerified') IS NULL
BEGIN
    EXEC sp_rename N'auth.UsersAccounts.PhoneNumberConfirmed', N'PhoneNumberVerified', N'COLUMN';
END
GO
