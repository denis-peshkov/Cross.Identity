CREATE TABLE [auth].[UsersCommunicationEndpoints]
(
    [UserCommunicationEndpointId] UNIQUEIDENTIFIER NOT NULL,
    [UserId]                      UNIQUEIDENTIFIER NOT NULL,
    [Channel]                     SMALLINT         NOT NULL,
    [Address]                     NVARCHAR(320)    NOT NULL,
    [IsVerified]                  BIT              NOT NULL,
    [Source]                      SMALLINT         NOT NULL,
    [SourceRefId]                 BIGINT           NULL,
    [IsPreferred]                 BIT              NOT NULL,
    [CreatedAt]                   DATETIME2(7)     NOT NULL,
    [UpdatedAt]                   DATETIME2(7)     NULL,
    [ConcurrencyStamp]            UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_UsersCommunicationEndpoints] PRIMARY KEY ([UserCommunicationEndpointId])
);
GO

CREATE UNIQUE INDEX [UX_auth_UsersCommunicationEndpoints_User_Channel_Address]
    ON [auth].[UsersCommunicationEndpoints]([UserId], [Channel], [Address]);
GO
CREATE INDEX [IX_auth_UsersCommunicationEndpoints_UserId]
    ON [auth].[UsersCommunicationEndpoints]([UserId]);
GO
CREATE UNIQUE INDEX [UX_auth_UsersCommunicationEndpoints_User_Preferred]
    ON [auth].[UsersCommunicationEndpoints]([UserId])
    WHERE [IsPreferred] = 1;
GO
