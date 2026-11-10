-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
-- Fail closed if Scheme 'facebook' is already owned by a non-Facebook Name.
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM auth."Providers"
        WHERE "Scheme" = 'facebook' AND "Name" <> 'Facebook'
    ) THEN
        RAISE EXCEPTION 'auth.Providers: Scheme facebook is already assigned to a non-Facebook Name. Resolve the collision before applying 5_01_auth_Providers_Facebook.';
    END IF;
END $$;

INSERT INTO auth."Providers" ("ProviderId", "Name", "Scheme", "IsEnabled", "CreatedAt")
SELECT 5, 'Facebook', 'facebook', TRUE, (NOW() AT TIME ZONE 'utc')
WHERE NOT EXISTS (
    SELECT 1
    FROM auth."Providers"
    WHERE "ProviderId" = 5 OR "Name" = 'Facebook'
);
