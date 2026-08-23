DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'auth'
          AND table_name = 'UsersExternalLogins'
          AND column_name = 'UserExternalLoginId'
          AND data_type = 'bigint'
    ) THEN
        CREATE TEMP TABLE "_UserExternalLoginIdMap"
        (
            "OldId" bigint NOT NULL PRIMARY KEY,
            "NewId" uuid     NOT NULL
        ) ON COMMIT DROP;

        INSERT INTO "_UserExternalLoginIdMap" ("OldId", "NewId")
        SELECT "UserExternalLoginId", gen_random_uuid()
        FROM auth."UsersExternalLogins";

        UPDATE auth."Audits" a
        SET "EntityId" = m."NewId"::text
        FROM "_UserExternalLoginIdMap" m
        WHERE a."EntityType" = 6
          AND a."EntityId" = m."OldId"::text;

        UPDATE auth."UsersCommunicationEndpoints" e
        SET "EntityId" = m."NewId",
            "UpdatedAt" = (NOW() AT TIME ZONE 'UTC')
        FROM auth."UsersExternalLogins" el
        INNER JOIN "_UserExternalLoginIdMap" m ON el."UserExternalLoginId" = m."OldId"
        WHERE e."UserAccountId" = el."UserAccountId"
          AND e."Source" = 1
          AND e."Channel" = 0
          AND el."ProviderEmail" IS NOT NULL
          AND lower(trim(e."Address")) = lower(trim(el."ProviderEmail"))
          AND (e."EntityId" IS NULL OR e."EntityId" <> m."NewId");

        ALTER TABLE auth."UsersExternalLogins"
            ADD COLUMN "UserExternalLoginIdNew" uuid NULL;

        UPDATE auth."UsersExternalLogins" el
        SET "UserExternalLoginIdNew" = m."NewId"
        FROM "_UserExternalLoginIdMap" m
        WHERE el."UserExternalLoginId" = m."OldId";

        ALTER TABLE auth."UsersExternalLogins" DROP CONSTRAINT "PK_auth_UsersExternalLogins";
        ALTER TABLE auth."UsersExternalLogins" DROP COLUMN "UserExternalLoginId";
        ALTER TABLE auth."UsersExternalLogins" RENAME COLUMN "UserExternalLoginIdNew" TO "UserExternalLoginId";
        ALTER TABLE auth."UsersExternalLogins" ALTER COLUMN "UserExternalLoginId" SET NOT NULL;
        ALTER TABLE auth."UsersExternalLogins"
            ADD CONSTRAINT "PK_auth_UsersExternalLogins" PRIMARY KEY ("UserExternalLoginId");

        IF EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'auth'
              AND table_name = 'UsersExternalLogins'
              AND column_name = 'LastUsedAt'
        ) AND NOT EXISTS (
            SELECT 1
            FROM information_schema.columns
            WHERE table_schema = 'auth'
              AND table_name = 'UsersExternalLogins'
              AND column_name = 'UpdatedAt'
        ) THEN
            ALTER TABLE auth."UsersExternalLogins" RENAME COLUMN "LastUsedAt" TO "UpdatedAt";
        END IF;
    END IF;
END $$;
