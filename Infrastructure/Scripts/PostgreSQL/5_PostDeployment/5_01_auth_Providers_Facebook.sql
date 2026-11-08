-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
INSERT INTO auth."Providers" ("ProviderId", "Name", "Scheme", "IsEnabled", "CreatedAt")
SELECT 5, 'Facebook', 'facebook', TRUE, (NOW() AT TIME ZONE 'utc')
WHERE NOT EXISTS (
    SELECT 1
    FROM auth."Providers"
    WHERE "ProviderId" = 5 OR "Name" = 'Facebook'
);
