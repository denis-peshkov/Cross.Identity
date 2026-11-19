-- Create auth.AccountMergePendings (Cross.Identity account merge pending token).
-- Idempotent for existing DBs; journals matching 2_Initial script when table is present.

IF OBJECT_ID(N'[auth].[AccountMergePendings]', N'U') IS NULL
BEGIN
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
END
GO

IF OBJECT_ID(N'[auth].[AccountMergePendings]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = N'UX_auth_AccountMergePendings_MergeToken'
         AND object_id = OBJECT_ID(N'[auth].[AccountMergePendings]')
   )
BEGIN
    CREATE UNIQUE INDEX [UX_auth_AccountMergePendings_MergeToken]
        ON [auth].[AccountMergePendings]([MergeToken]);
END
GO

-- Journal 2_03 even when the table already existed (heal PreDeployment without journal).
IF OBJECT_ID(N'[dbo].[__MigrationsHistory]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[auth].[AccountMergePendings]', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [dbo].[__MigrationsHistory]
       WHERE [ScriptName] = N'2_03_auth_AccountMergePendings.sql'
   )
BEGIN
    INSERT INTO [dbo].[__MigrationsHistory] ([ScriptName], [Applied])
    VALUES (N'2_03_auth_AccountMergePendings.sql', SYSUTCDATETIME());
END
GO
