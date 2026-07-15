CREATE TABLE [auth].[UsersExternalLogins]
(
    [UserExternalLoginId] BIGINT IDENTITY (1,1) NOT NULL,
    [UserAccountId]       UNIQUEIDENTIFIER      NOT NULL,
    [ProviderId]          SMALLINT              NOT NULL,
    [ProviderUserId]      NVARCHAR(200)         NOT NULL,
    [ProviderEmail]       NVARCHAR(200)         NULL,
    [DisplayName]         NVARCHAR(200)         NULL,
    [AvatarUrl]           NVARCHAR(500)         NULL,
    [ProfileUrl]          NVARCHAR(500)         NULL,
    [AccessTokenEnc]      VARBINARY(MAX)        NULL,
    [RefreshTokenEnc]     VARBINARY(MAX)        NULL,
    [ExpiresAt]           DATETIME2(7)          NULL,
    [Scope]               NVARCHAR(500)         NULL,
    [CreatedAt]           DATETIME2(7)          NOT NULL,
    [LastUsedAt]          DATETIME2(7)          NULL,

    CONSTRAINT [PK_auth_UsersExternalLogins] PRIMARY KEY ([UserExternalLoginId])
);
GO

CREATE UNIQUE INDEX [UX_auth_UsersExternalLogins_Provider_User] ON [auth].[UsersExternalLogins]([ProviderId],[ProviderUserId]);
GO
CREATE INDEX [IX_auth_UsersExternalLogins_UserAccountId]        ON [auth].[UsersExternalLogins]([UserAccountId]);
GO
CREATE INDEX [IX_auth_UsersExternalLogins_ProviderUserId]       ON [auth].[UsersExternalLogins]([ProviderUserId]);
GO
