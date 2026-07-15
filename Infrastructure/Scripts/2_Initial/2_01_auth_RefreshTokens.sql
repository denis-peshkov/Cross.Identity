CREATE TABLE [auth].[RefreshTokens]
(
    [RefreshTokenId]    UNIQUEIDENTIFIER NOT NULL,
    [FamilyId]          UNIQUEIDENTIFIER NOT NULL,
    [UserId]            UNIQUEIDENTIFIER NOT NULL,
    [TokenHash]         NVARCHAR(64)     NOT NULL,
    [ExpiresAt]         DATETIME2(7)     NOT NULL,
    [AbsoluteExpiresAt] DATETIME2(7)     NOT NULL,
    [CreatedAt]         DATETIME2(7)     NOT NULL,
    [ReplacedByTokenId] UNIQUEIDENTIFIER NULL,
    [RevokedAt]         DATETIME2(7)     NULL,
    [RevokeReason]      SMALLINT         NULL,
    [RevokedByIp]       NVARCHAR(45)     NULL,
    [DeviceFingerprint] NVARCHAR(100)    NULL,
    [UserAgent]         NVARCHAR(512)    NULL,
    [IpAddress]         NVARCHAR(45)     NULL,
    [RowVersion]        ROWVERSION       NOT NULL,

    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([RefreshTokenId] ASC)
)
GO

CREATE INDEX [IX_auth_RefreshTokens_User] ON [auth].[RefreshTokens] ([UserId]);
GO
CREATE INDEX [IX_auth_RefreshTokens_TokenHash] ON [auth].[RefreshTokens] ([TokenHash]);
GO
CREATE INDEX [IX_auth_RefreshTokens_Expires] ON [auth].[RefreshTokens] ([ExpiresAt]);
GO
CREATE INDEX [IX_auth_RefreshTokens_Revoked] ON [auth].[RefreshTokens] ([RevokedAt]);
GO
