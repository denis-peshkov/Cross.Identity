CREATE TABLE [auth].[AccessTokens]
(
    [AccessTokenId]    UNIQUEIDENTIFIER NOT NULL,
    [FamilyId]         UNIQUEIDENTIFIER NOT NULL,
    [UserAccountId]    UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]        NVARCHAR(64)     NOT NULL,
    [ExpiresAt]        DATETIME2(7)     NOT NULL,
    [CreatedAt]        DATETIME2(7)     NOT NULL,
    [RevokedAt]        DATETIME2(7)     NULL,
    [ConcurrencyStamp] UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_AccessTokens] PRIMARY KEY CLUSTERED ([AccessTokenId] ASC)
);
GO

CREATE INDEX [IX_auth_AccessTokens_UserAccountId] ON [auth].[AccessTokens] ([UserAccountId]);
GO
CREATE INDEX [IX_auth_AccessTokens_TokenHash] ON [auth].[AccessTokens] ([TokenHash]);
GO
