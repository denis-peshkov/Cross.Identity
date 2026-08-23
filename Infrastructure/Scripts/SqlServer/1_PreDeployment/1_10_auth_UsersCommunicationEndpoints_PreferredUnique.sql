;WITH ranked AS (
    SELECT [UserCommunicationEndpointId],
           ROW_NUMBER() OVER (
               PARTITION BY [UserAccountId]
               ORDER BY COALESCE([UpdatedAt], [CreatedAt]) DESC, [UserCommunicationEndpointId]
           ) AS rn
    FROM [auth].[UsersCommunicationEndpoints]
    WHERE [IsPreferred] = 1
)
UPDATE e
SET [IsPreferred] = 0,
    [UpdatedAt] = SYSUTCDATETIME()
FROM [auth].[UsersCommunicationEndpoints] e
INNER JOIN ranked r ON e.[UserCommunicationEndpointId] = r.[UserCommunicationEndpointId]
WHERE r.rn > 1;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_auth_UsersCommunicationEndpoints_User_Preferred'
      AND object_id = OBJECT_ID(N'auth.UsersCommunicationEndpoints')
)
BEGIN
    CREATE UNIQUE INDEX [UX_auth_UsersCommunicationEndpoints_User_Preferred]
        ON [auth].[UsersCommunicationEndpoints]([UserAccountId])
        WHERE [IsPreferred] = 1;
END
GO
