CREATE TABLE [auth].[ExternalLoginStates]
(
    [ExternalLoginStateId] BIGINT IDENTITY (1,1) NOT NULL,
    [Nonce]                NVARCHAR(32)          NOT NULL,
    [Provider]             NVARCHAR(64)          NOT NULL,
    [ReturnUrl]            NVARCHAR(512)         NULL,
    [LinkUserId]           UNIQUEIDENTIFIER      NULL,
    [ExpiresAt]            DATETIME2(7)          NOT NULL,
    [CreatedAt]            DATETIME2(7)          NOT NULL,
    [ConcurrencyStamp]     UNIQUEIDENTIFIER      NOT NULL,

    CONSTRAINT [PK_auth_ExternalLoginStates] PRIMARY KEY ([ExternalLoginStateId])
);
GO

CREATE UNIQUE INDEX [UX_auth_ExternalLoginStates_Nonce] ON [auth].[ExternalLoginStates]([Nonce]);
GO
CREATE INDEX [IX_auth_ExternalLoginStates_ExpiresAt] ON [auth].[ExternalLoginStates]([ExpiresAt]);
GO
