ALTER TABLE auth."RefreshTokens"
    ADD COLUMN "CreatedIpAddress"         varchar(64)  NULL,
    ADD COLUMN "CreatedUserAgent"         varchar(512) NULL,
    ADD COLUMN "CreatedDeviceFingerprint" varchar(128) NULL;
