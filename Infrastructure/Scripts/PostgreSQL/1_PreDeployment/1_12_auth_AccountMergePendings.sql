-- Create auth.AccountMergePendings (Cross.Identity account merge pending token).
-- Idempotent for existing DBs; journals matching 2_Initial script when table is present.

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'auth'
          AND table_name = 'AccountMergePendings'
    ) THEN
        CREATE TABLE auth."AccountMergePendings"
        (
            "AccountMergePendingId" uuid         NOT NULL,
            "MergeToken"           varchar(64)  NOT NULL,
            "SurvivorUserAccountId" uuid        NOT NULL,
            "AbsorbedUserAccountId" uuid        NOT NULL,
            "ExpiresAt"            timestamp without time zone NOT NULL,
            "CreatedAt"            timestamp without time zone NOT NULL,
            "ConsumedAt"           timestamp without time zone NULL,
            "ConcurrencyStamp"     uuid         NOT NULL,

            CONSTRAINT "PK_auth_AccountMergePendings" PRIMARY KEY ("AccountMergePendingId"),
            CONSTRAINT "FK_auth_AccountMergePendings_Survivor"
                FOREIGN KEY ("SurvivorUserAccountId") REFERENCES auth."UsersAccounts" ("UserAccountId"),
            CONSTRAINT "FK_auth_AccountMergePendings_Absorbed"
                FOREIGN KEY ("AbsorbedUserAccountId") REFERENCES auth."UsersAccounts" ("UserAccountId")
        );
    END IF;

    CREATE UNIQUE INDEX IF NOT EXISTS "UX_auth_AccountMergePendings_MergeToken"
        ON auth."AccountMergePendings" ("MergeToken");

    -- Journal 2_03 even when the table already existed (heal PreDeployment without journal).
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = '__MigrationsHistory'
    )
    AND NOT EXISTS (
        SELECT 1
        FROM public."__MigrationsHistory"
        WHERE "ScriptName" = '2_03_auth_AccountMergePendings.sql'
    ) THEN
        INSERT INTO public."__MigrationsHistory" ("ScriptName", "Applied")
        VALUES ('2_03_auth_AccountMergePendings.sql', (NOW() AT TIME ZONE 'utc'));
    END IF;
END $$;
