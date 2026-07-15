CREATE TABLE [auth].[AccessTokens]
(
    [AccessTokenId]     UNIQUEIDENTIFIER NOT NULL,
    [FamilyId]          UNIQUEIDENTIFIER NOT NULL,
    [UserId]            UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]         NVARCHAR(64)     NOT NULL,
    [ExpiresAt]         DATETIME2(7)     NOT NULL,
    [CreatedAt]         DATETIME2(7)     NOT NULL,
    [RevokedAt]         DATETIME2(7)     NULL,
    [RevokeReason]      SMALLINT         NULL,
    [RevokedByIp]       NVARCHAR(45)     NULL,
    [DeviceFingerprint] NVARCHAR(100)    NULL,
    [UserAgent]         NVARCHAR(512)    NULL,
    [IpAddress]         NVARCHAR(45)     NULL,

    CONSTRAINT [PK_AccessTokens] PRIMARY KEY CLUSTERED ([AccessTokenId] ASC)
)
GO

CREATE INDEX [IX_auth_AccessTokens_User] ON [auth].[AccessTokens] ([UserId]);
GO
CREATE INDEX [IX_auth_AccessTokens_TokenHash] ON [auth].[AccessTokens] ([TokenHash]);
GO
CREATE INDEX [IX_auth_AccessTokens_Expires] ON [auth].[AccessTokens] ([ExpiresAt]);
GO
CREATE INDEX [IX_auth_AccessTokens_Revoked] ON [auth].[AccessTokens] ([RevokedAt]);
GO
