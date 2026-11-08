-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
IF NOT EXISTS (
    SELECT 1
    FROM [auth].[Providers]
    WHERE [ProviderId] = 5 OR [Name] = N'Facebook'
)
BEGIN
    INSERT INTO [auth].[Providers] ([ProviderId], [Name], [Scheme], [IsEnabled], [CreatedAt])
    VALUES (5, N'Facebook', N'facebook', 1, SYSUTCDATETIME());
END
GO
