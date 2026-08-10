CREATE TABLE [auth].[RefreshTokens]
(
    [RefreshTokenId]    UNIQUEIDENTIFIER NOT NULL,
    [FamilyId]          UNIQUEIDENTIFIER NOT NULL,
    [UserAccountId]     UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]         NVARCHAR(64)     NOT NULL,
    [ExpiresAt]         DATETIME2(7)     NOT NULL,
    [AbsoluteExpiresAt] DATETIME2(7)     NOT NULL,
    [CreatedAt]         DATETIME2(7)     NOT NULL,
    [ReplacedByTokenId] UNIQUEIDENTIFIER NULL,
    [RevokedAt]         DATETIME2(7)     NULL,
    [ConcurrencyStamp]  UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_RefreshTokens] PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC)
);
GO

CREATE INDEX [IX_auth_RefreshTokens_UserAccountId] ON [auth].[RefreshTokens] ([UserAccountId]);
GO
CREATE INDEX [IX_auth_RefreshTokens_TokenHash] ON [auth].[RefreshTokens] ([TokenHash]);
GO
