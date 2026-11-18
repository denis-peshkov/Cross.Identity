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

        CREATE UNIQUE INDEX "UX_auth_AccountMergePendings_MergeToken"
            ON auth."AccountMergePendings" ("MergeToken");
    END IF;
END $$;
