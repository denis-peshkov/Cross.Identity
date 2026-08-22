ALTER TABLE [auth].[RefreshTokens] ADD [LastActivityAt] DATETIME2(7) NULL;
GO

UPDATE [auth].[RefreshTokens]
SET [LastActivityAt] = [CreatedAt]
WHERE [LastActivityAt] IS NULL;
GO

ALTER TABLE [auth].[RefreshTokens] ALTER COLUMN [LastActivityAt] DATETIME2(7) NOT NULL;
GO
