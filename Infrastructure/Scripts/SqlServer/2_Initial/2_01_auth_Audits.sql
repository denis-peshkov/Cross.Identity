CREATE TABLE [auth].[Audits]
(
    [AuditId]           UNIQUEIDENTIFIER NOT NULL,
    [UserAccountId]     UNIQUEIDENTIFIER NOT NULL,
    [Operation]         SMALLINT         NOT NULL,
    [EntityType]        SMALLINT         NOT NULL,
    [EntityId]          NVARCHAR(64)     NULL,
    [RevokedReason]     SMALLINT         NULL,
    [IpAddress]         NVARCHAR(64)     NULL,
    [UserAgent]         NVARCHAR(512)    NULL,
    [DeviceFingerprint] NVARCHAR(128)    NULL,
    [Notes]             NVARCHAR(2000)   NULL,
    [CreatedAt]         DATETIME2(7)     NOT NULL,

    CONSTRAINT [PK_auth_Audits] PRIMARY KEY ([AuditId])
);
GO

CREATE INDEX [IX_auth_Audits_CreatedAt] ON [auth].[Audits]([CreatedAt]);
GO
CREATE INDEX [IX_auth_Audits_UserAccountId] ON [auth].[Audits]([UserAccountId]);
GO
CREATE INDEX [IX_auth_Audits_Operation] ON [auth].[Audits]([Operation]);
GO
CREATE INDEX [IX_auth_Audits_Entity] ON [auth].[Audits]([EntityType], [EntityId]);
GO
