-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
-- PostgreSQL: Name unique is case-sensitive — reconcile any LOWER(Name)='facebook' into one row
-- (preserve UsersExternalLogins), then insert only if Facebook is still absent.
DO $$
DECLARE
    survivor smallint;
    other_id smallint;
BEGIN
    -- Fail closed: Scheme 'facebook' owned by a Name that is not Facebook (case-insensitive).
    IF EXISTS (
        SELECT 1
        FROM auth."Providers"
        WHERE "Scheme" = 'facebook' AND LOWER("Name") <> 'facebook'
    ) THEN
        RAISE EXCEPTION 'auth.Providers: Scheme facebook is already assigned to a non-Facebook Name. Resolve the collision before applying 5_01_auth_Providers_Facebook.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM auth."Providers"
        WHERE LOWER("Name") = 'facebook'
    ) THEN
        SELECT COALESCE(
            (
                SELECT p."ProviderId"
                FROM auth."Providers" AS p
                WHERE LOWER(p."Name") = 'facebook' AND p."ProviderId" = 5
                LIMIT 1
            ),
            (
                SELECT MIN(p."ProviderId")
                FROM auth."Providers" AS p
                WHERE LOWER(p."Name") = 'facebook'
            )
        )
        INTO survivor;

        FOR other_id IN
            SELECT p."ProviderId"
            FROM auth."Providers" AS p
            WHERE LOWER(p."Name") = 'facebook' AND p."ProviderId" <> survivor
        LOOP
            -- Drop login rows that would collide on (ProviderId, ProviderUserId) after remap.
            DELETE FROM auth."UsersExternalLogins" AS u
            WHERE u."ProviderId" = other_id
              AND EXISTS (
                  SELECT 1
                  FROM auth."UsersExternalLogins" AS s
                  WHERE s."ProviderId" = survivor
                    AND s."ProviderUserId" = u."ProviderUserId"
              );

            UPDATE auth."UsersExternalLogins"
            SET "ProviderId" = survivor
            WHERE "ProviderId" = other_id;

            DELETE FROM auth."Providers"
            WHERE "ProviderId" = other_id;
        END LOOP;

        UPDATE auth."Providers"
        SET
            "Name" = 'Facebook',
            "Scheme" = 'facebook',
            "IsEnabled" = TRUE
        WHERE "ProviderId" = survivor;
    END IF;
END $$;

INSERT INTO auth."Providers" ("ProviderId", "Name", "Scheme", "IsEnabled", "CreatedAt")
SELECT 5, 'Facebook', 'facebook', TRUE, (NOW() AT TIME ZONE 'utc')
WHERE NOT EXISTS (
    SELECT 1
    FROM auth."Providers"
    WHERE "ProviderId" = 5 OR LOWER("Name") = 'facebook'
);
