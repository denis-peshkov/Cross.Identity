UPDATE auth.`UsersCommunicationEndpoints` e
INNER JOIN (
    SELECT UserCommunicationEndpointId
    FROM (
        SELECT UserCommunicationEndpointId,
               ROW_NUMBER() OVER (
                   PARTITION BY UserAccountId
                   ORDER BY COALESCE(UpdatedAt, CreatedAt) DESC, UserCommunicationEndpointId
               ) AS rn
        FROM auth.`UsersCommunicationEndpoints`
        WHERE IsPreferred = 1
    ) ranked
    WHERE rn > 1
) dup ON e.UserCommunicationEndpointId = dup.UserCommunicationEndpointId
SET e.IsPreferred = 0,
    e.UpdatedAt = UTC_TIMESTAMP(6);

CREATE UNIQUE INDEX `UX_auth_UsersCommunicationEndpoints_User_Preferred`
    ON auth.`UsersCommunicationEndpoints` ((IF(`IsPreferred`, `UserAccountId`, NULL)));
