ALTER TABLE `auth`.`RefreshTokens`
    ADD COLUMN `CreatedIpAddress`         VARCHAR(64)  NULL,
    ADD COLUMN `CreatedUserAgent`         VARCHAR(512) NULL,
    ADD COLUMN `CreatedDeviceFingerprint` VARCHAR(128) NULL;
