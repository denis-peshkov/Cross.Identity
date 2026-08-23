CREATE TABLE [auth].[UsersCommunicationEndpoints]
(
    [UserCommunicationEndpointId] UNIQUEIDENTIFIER NOT NULL,
    [UserAccountId]               UNIQUEIDENTIFIER NOT NULL,
    [Channel]                     SMALLINT         NOT NULL,
    [Address]                     NVARCHAR(320)    NOT NULL,
    [IsVerified]                  BIT              NOT NULL,
    [Source]                      SMALLINT         NOT NULL,
    [EntityId]                    UNIQUEIDENTIFIER NULL,
    [IsPreferred]                 BIT              NOT NULL,
    [CreatedAt]                   DATETIME2(7)     NOT NULL,
    [UpdatedAt]                   DATETIME2(7)     NULL,
    [ConcurrencyStamp]            UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_UsersCommunicationEndpoints] PRIMARY KEY ([UserCommunicationEndpointId])
);
GO

CREATE UNIQUE INDEX [UX_auth_UsersCommunicationEndpoints_User_Channel_Address]
    ON [auth].[UsersCommunicationEndpoints]([UserAccountId], [Channel], [Address]);
GO
CREATE UNIQUE INDEX [UX_auth_UsersCommunicationEndpoints_User_Preferred]
    ON [auth].[UsersCommunicationEndpoints]([UserAccountId])
    WHERE [IsPreferred] = 1;
GO
CREATE INDEX [IX_auth_UsersCommunicationEndpoints_UserAccountId]
    ON [auth].[UsersCommunicationEndpoints]([UserAccountId]);
GO
CREATE INDEX [IX_auth_UsersCommunicationEndpoints_EntityId]
    ON [auth].[UsersCommunicationEndpoints]([EntityId]);
GO
