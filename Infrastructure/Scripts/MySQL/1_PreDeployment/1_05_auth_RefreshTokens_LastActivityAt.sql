ALTER TABLE `auth`.`RefreshTokens`
    ADD COLUMN `LastActivityAt` DATETIME(6) NULL;

UPDATE `auth`.`RefreshTokens`
SET `LastActivityAt` = `CreatedAt`
WHERE `LastActivityAt` IS NULL;

ALTER TABLE `auth`.`RefreshTokens`
    MODIFY COLUMN `LastActivityAt` DATETIME(6) NOT NULL;
