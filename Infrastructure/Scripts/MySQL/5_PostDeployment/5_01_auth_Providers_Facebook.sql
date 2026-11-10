-- Add Facebook to auth.Providers for databases that already applied 4_01_auth_Providers.sql.
-- Fail closed if Scheme 'facebook' is already owned by a non-Facebook Name (do not INSERT IGNORE that collision).
SET @facebook_scheme_collision := (
    SELECT COUNT(*)
    FROM `auth`.`Providers`
    WHERE `Scheme` = 'facebook' AND `Name` <> 'Facebook'
);

-- STRICT mode: CAST of the diagnostic string to UNSIGNED fails the script with a visible message.
SET @sql := IF(
    @facebook_scheme_collision > 0,
    'SELECT CAST(''auth.Providers: Scheme facebook is already assigned to a non-Facebook Name. Resolve the collision before applying 5_01_auth_Providers_Facebook.'' AS UNSIGNED)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT INTO `auth`.`Providers` (`ProviderId`, `Name`, `Scheme`, `IsEnabled`, `CreatedAt`)
SELECT 5, 'Facebook', 'facebook', 1, UTC_TIMESTAMP(6)
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1
    FROM `auth`.`Providers`
    WHERE `ProviderId` = 5 OR `Name` = 'Facebook'
);
