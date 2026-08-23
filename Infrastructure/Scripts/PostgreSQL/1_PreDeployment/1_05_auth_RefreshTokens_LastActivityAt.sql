ALTER TABLE auth."RefreshTokens"
    ADD COLUMN "LastActivityAt" timestamp without time zone NULL;

UPDATE auth."RefreshTokens"
SET "LastActivityAt" = "CreatedAt"
WHERE "LastActivityAt" IS NULL;

ALTER TABLE auth."RefreshTokens"
    ALTER COLUMN "LastActivityAt" SET NOT NULL;
