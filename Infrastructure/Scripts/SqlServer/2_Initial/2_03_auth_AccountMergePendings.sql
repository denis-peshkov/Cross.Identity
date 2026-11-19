CREATE TABLE [auth].[AccountMergePendings]
(
    [AccountMergePendingId] UNIQUEIDENTIFIER NOT NULL,
    [MergeToken]            NVARCHAR(64)     NOT NULL,
    [SurvivorUserAccountId] UNIQUEIDENTIFIER NOT NULL,
    [AbsorbedUserAccountId] UNIQUEIDENTIFIER NOT NULL,
    [ExpiresAt]             DATETIME2(7)     NOT NULL,
    [CreatedAt]             DATETIME2(7)     NOT NULL,
    [ConsumedAt]            DATETIME2(7)     NULL,
    [ConcurrencyStamp]      UNIQUEIDENTIFIER NOT NULL,

    CONSTRAINT [PK_auth_AccountMergePendings] PRIMARY KEY ([AccountMergePendingId]),
    CONSTRAINT [FK_auth_AccountMergePendings_Survivor]
        FOREIGN KEY ([SurvivorUserAccountId]) REFERENCES [auth].[UsersAccounts] ([UserAccountId]),
    CONSTRAINT [FK_auth_AccountMergePendings_Absorbed]
        FOREIGN KEY ([AbsorbedUserAccountId]) REFERENCES [auth].[UsersAccounts] ([UserAccountId])
);
GO

CREATE UNIQUE INDEX [UX_auth_AccountMergePendings_MergeToken]
    ON [auth].[AccountMergePendings]([MergeToken]);
GO
