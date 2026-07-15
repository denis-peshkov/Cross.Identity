CREATE TABLE [auth].[EmailVerifications]
(
    [EmailVerificationId] BIGINT IDENTITY (1,1) NOT NULL,
    [UserAccountId]       UNIQUEIDENTIFIER      NOT NULL,
    [Email]               NVARCHAR(320)         NOT NULL,
    [TokenHash]           BINARY(32)            NOT NULL,
    [TokenLength]         TINYINT               NOT NULL,
    [Attempts]            TINYINT               NOT NULL,
    [MaxAttempts]         TINYINT               NOT NULL,
    [ExpiresAt]           DATETIME2(7)          NOT NULL,
    [UsedAt]              DATETIME2(7)          NULL,
    [CreatedAt]           DATETIME2(7)          NOT NULL,

    CONSTRAINT [PK_auth_EmailVerifications] PRIMARY KEY ([EmailVerificationId])
);
GO

CREATE INDEX [IX_auth_EmailVerifications_UserAccount] ON [auth].[EmailVerifications]([UserAccountId]);
GO
CREATE INDEX [IX_auth_EmailVerifications_Email]       ON [auth].[EmailVerifications]([Email]);
GO
CREATE INDEX [IX_auth_EmailVerifications_TokenHash] ON [auth].[EmailVerifications]([TokenHash]);
GO
CREATE INDEX [IX_auth_EmailVerifications_ExpiresAt] ON [auth].[EmailVerifications]([ExpiresAt]);
GO
