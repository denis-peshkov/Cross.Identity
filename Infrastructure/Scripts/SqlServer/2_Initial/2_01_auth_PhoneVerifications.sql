CREATE TABLE [auth].[PhoneVerifications]
(
    [PhoneVerificationId] UNIQUEIDENTIFIER NOT NULL,
    [UserAccountId]       UNIQUEIDENTIFIER NOT NULL,
    [PhoneNumber]         NVARCHAR(20)     NOT NULL,
    [CodeHash]            BINARY(32)       NOT NULL,
    [CodeLength]          TINYINT          NOT NULL,
    [Attempts]            TINYINT          NOT NULL,
    [MaxAttempts]         TINYINT          NOT NULL,
    [ExpiresAt]           DATETIME2(7)     NOT NULL,
    [UsedAt]              DATETIME2(7)     NULL,
    [CreatedAt]           DATETIME2(7)     NOT NULL,
    [ConcurrencyStamp]    UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_PhoneVerifications] PRIMARY KEY ([PhoneVerificationId])
);
GO

CREATE INDEX [IX_auth_PhoneVerifications_UserAccount] ON [auth].[PhoneVerifications]([UserAccountId]);
GO
CREATE INDEX [IX_auth_PhoneVerifications_CodeHash] ON [auth].[PhoneVerifications]([CodeHash]);
GO
CREATE INDEX [IX_auth_PhoneVerifications_ExpiresAt] ON [auth].[PhoneVerifications]([ExpiresAt]);
GO
