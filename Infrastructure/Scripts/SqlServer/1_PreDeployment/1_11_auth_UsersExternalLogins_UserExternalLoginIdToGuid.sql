IF OBJECT_ID(N'auth.UsersExternalLogins', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.columns c
       INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
       WHERE c.object_id = OBJECT_ID(N'auth.UsersExternalLogins')
         AND c.name = N'UserExternalLoginId'
         AND t.name = N'bigint'
   )
BEGIN
    CREATE TABLE #UserExternalLoginIdMap
    (
        OldId BIGINT           NOT NULL PRIMARY KEY,
        NewId UNIQUEIDENTIFIER NOT NULL
    );

    INSERT INTO #UserExternalLoginIdMap (OldId, NewId)
    SELECT [UserExternalLoginId], NEWID()
    FROM [auth].[UsersExternalLogins];

    UPDATE a
    SET [EntityId] = CAST(m.NewId AS NVARCHAR(64))
    FROM [auth].[Audits] a
    INNER JOIN #UserExternalLoginIdMap m ON a.[EntityId] = CAST(m.OldId AS NVARCHAR(64))
    WHERE a.[EntityType] = 6;

    UPDATE e
    SET [EntityId] = m.NewId,
        [UpdatedAt] = SYSUTCDATETIME()
    FROM [auth].[UsersCommunicationEndpoints] e
    INNER JOIN [auth].[UsersExternalLogins] el ON e.[UserAccountId] = el.[UserAccountId]
    INNER JOIN #UserExternalLoginIdMap m ON el.[UserExternalLoginId] = m.OldId
    WHERE e.[Source] = 1
      AND e.[Channel] = 0
      AND el.[ProviderEmail] IS NOT NULL
      AND LOWER(LTRIM(RTRIM(e.[Address]))) = LOWER(LTRIM(RTRIM(el.[ProviderEmail])))
      AND (e.[EntityId] IS NULL OR e.[EntityId] <> m.NewId);

    ALTER TABLE [auth].[UsersExternalLogins]
        ADD [UserExternalLoginIdNew] UNIQUEIDENTIFIER NULL;

    UPDATE el
    SET [UserExternalLoginIdNew] = m.NewId
    FROM [auth].[UsersExternalLogins] el
    INNER JOIN #UserExternalLoginIdMap m ON el.[UserExternalLoginId] = m.OldId;

    ALTER TABLE [auth].[UsersExternalLogins] DROP CONSTRAINT [PK_auth_UsersExternalLogins];

    ALTER TABLE [auth].[UsersExternalLogins] DROP COLUMN [UserExternalLoginId];

    EXEC sp_rename N'auth.UsersExternalLogins.UserExternalLoginIdNew', N'UserExternalLoginId', N'COLUMN';

    ALTER TABLE [auth].[UsersExternalLogins]
        ALTER COLUMN [UserExternalLoginId] UNIQUEIDENTIFIER NOT NULL;

    ALTER TABLE [auth].[UsersExternalLogins]
        ADD CONSTRAINT [PK_auth_UsersExternalLogins] PRIMARY KEY ([UserExternalLoginId]);

    IF COL_LENGTH(N'auth.UsersExternalLogins', N'LastUsedAt') IS NOT NULL
       AND COL_LENGTH(N'auth.UsersExternalLogins', N'UpdatedAt') IS NULL
    BEGIN
        EXEC sp_rename N'auth.UsersExternalLogins.LastUsedAt', N'UpdatedAt', N'COLUMN';
    END

    DROP TABLE #UserExternalLoginIdMap;
END
GO
