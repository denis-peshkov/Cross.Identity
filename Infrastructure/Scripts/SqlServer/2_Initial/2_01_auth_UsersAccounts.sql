CREATE TABLE [auth].[UsersAccounts]
(
    [UserAccountId]         UNIQUEIDENTIFIER  NOT NULL,

    [UserName]              NVARCHAR(200)     NULL,
    [NormalizedUserName]    NVARCHAR(200)     NULL,
    [Email]                 NVARCHAR(200)     NULL,
    [PhoneNumber]           NVARCHAR(20)      NULL,
    [PasswordPhc]           NVARCHAR(800)     NULL,
    [PasswordHash]          BINARY(32)        NULL,
    [PasswordSalt]          NVARCHAR(200)     NULL,
    [PasswordPepperVersion] SMALLINT          NOT NULL,

    [LockoutEnd]            DATETIMEOFFSET(0) NULL,
    [LockoutEnabled]        BIT               NOT NULL,
    [AccessFailedCount]     INT               NOT NULL,

    [SecurityStamp]         UNIQUEIDENTIFIER  NULL,
    [ConcurrencyStamp]      UNIQUEIDENTIFIER  NOT NULL,

    [EmailVerified]        BIT               NOT NULL,
    [PhoneNumberVerified]  BIT               NOT NULL,
    [TwoFactorEnabled]      BIT               NOT NULL,

    [IsActive]              BIT               NOT NULL,

    [CreatedAt]             DATETIME2(7)      NOT NULL,
    [LastLoginAt]           DATETIME2(7)      NULL,

    CONSTRAINT [PK_auth_UsersAccounts] PRIMARY KEY ([UserAccountId])
);
GO

CREATE UNIQUE INDEX [UX_auth_UsersAccounts_UserName]
    ON [auth].[UsersAccounts]([NormalizedUserName])
    WHERE [NormalizedUserName] IS NOT NULL;
GO
CREATE UNIQUE INDEX [UX_auth_UsersAccounts_Email]
    ON [auth].[UsersAccounts]([Email])
    WHERE [Email] IS NOT NULL AND [EmailVerified] = 1;
GO
CREATE UNIQUE INDEX [UX_auth_UsersAccounts_Phone]
    ON [auth].[UsersAccounts]([PhoneNumber])
    WHERE [PhoneNumber] IS NOT NULL AND [PhoneNumberVerified] = 1;
GO
