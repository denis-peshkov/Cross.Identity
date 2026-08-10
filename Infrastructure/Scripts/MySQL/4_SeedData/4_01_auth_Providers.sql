INSERT IGNORE INTO `auth`.`Providers` (`ProviderId`, `Name`, `Scheme`, `IsEnabled`, `CreatedAt`)
VALUES
    (1, 'Google', 'google', 1, UTC_TIMESTAMP(6)),
    (2, 'Apple', 'apple', 1, UTC_TIMESTAMP(6)),
    (3, 'Microsoft', 'microsoft', 1, UTC_TIMESTAMP(6)),
    (4, 'GitHub', 'github', 1, UTC_TIMESTAMP(6));
