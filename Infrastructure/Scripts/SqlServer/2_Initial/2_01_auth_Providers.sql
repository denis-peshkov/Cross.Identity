CREATE TABLE [auth].[Providers]
(
    [ProviderId] SMALLINT      NOT NULL,
    [Name]       NVARCHAR(50)  NOT NULL,
    [Scheme]     NVARCHAR(100) NOT NULL,
    [IsEnabled]  BIT           NOT NULL,
    [CreatedAt]  DATETIME2(7)  NOT NULL,
    [ConcurrencyStamp] UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_Providers] PRIMARY KEY ([ProviderId])
);
GO

CREATE UNIQUE INDEX [UX_auth_Providers_Name]   ON [auth].[Providers] ([Name]);
GO
CREATE UNIQUE INDEX [UX_auth_Providers_Scheme] ON [auth].[Providers] ([Scheme]);
GO
