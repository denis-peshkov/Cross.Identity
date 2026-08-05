INSERT IGNORE INTO `auth`.`Providers` (`ProviderId`, `Name`, `Scheme`, `IsEnabled`, `CreatedAt`, `ConcurrencyStamp`)
VALUES
    (1, 'Google', 'google', 1, UTC_TIMESTAMP(6), UUID()),
    (2, 'Apple', 'apple', 1, UTC_TIMESTAMP(6), UUID()),
    (3, 'Microsoft', 'microsoft', 1, UTC_TIMESTAMP(6), UUID()),
    (4, 'GitHub', 'github', 1, UTC_TIMESTAMP(6), UUID());
