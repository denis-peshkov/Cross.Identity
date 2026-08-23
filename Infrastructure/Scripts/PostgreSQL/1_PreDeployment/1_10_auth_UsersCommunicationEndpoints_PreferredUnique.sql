WITH ranked AS (
    SELECT "UserCommunicationEndpointId",
           ROW_NUMBER() OVER (
               PARTITION BY "UserAccountId"
               ORDER BY COALESCE("UpdatedAt", "CreatedAt") DESC, "UserCommunicationEndpointId"
           ) AS rn
    FROM auth."UsersCommunicationEndpoints"
    WHERE "IsPreferred" = true
)
UPDATE auth."UsersCommunicationEndpoints" e
SET "IsPreferred" = false,
    "UpdatedAt" = (NOW() AT TIME ZONE 'UTC')
FROM ranked r
WHERE e."UserCommunicationEndpointId" = r."UserCommunicationEndpointId"
  AND r.rn > 1;

CREATE UNIQUE INDEX IF NOT EXISTS "UX_auth_UsersCommunicationEndpoints_User_Preferred"
    ON auth."UsersCommunicationEndpoints" ("UserAccountId")
    WHERE "IsPreferred" = true;
