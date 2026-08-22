ALTER TABLE [auth].[RefreshTokens] ADD
    [CreatedIpAddress]         NVARCHAR(64)  NULL,
    [CreatedUserAgent]         NVARCHAR(512) NULL,
    [CreatedDeviceFingerprint] NVARCHAR(128) NULL;
GO
