-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
-- Fail closed if Scheme 'facebook' is already owned by a non-Facebook Name.
IF EXISTS (
    SELECT 1
    FROM [auth].[Providers]
    WHERE [Scheme] = N'facebook' AND [Name] <> N'Facebook'
)
BEGIN
    THROW 50001, 'auth.Providers: Scheme facebook is already assigned to a non-Facebook Name. Resolve the collision before applying 5_01_auth_Providers_Facebook.', 1;
END

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
