INSERT INTO auth."Providers" ("ProviderId", "Name", "Scheme", "IsEnabled", "CreatedAt", "ConcurrencyStamp")
VALUES
    (1, 'Google', 'google', TRUE, (NOW() AT TIME ZONE 'utc'), gen_random_uuid()),
    (2, 'Apple', 'apple', TRUE, (NOW() AT TIME ZONE 'utc'), gen_random_uuid()),
    (3, 'Microsoft', 'microsoft', TRUE, (NOW() AT TIME ZONE 'utc'), gen_random_uuid()),
    (4, 'GitHub', 'github', TRUE, (NOW() AT TIME ZONE 'utc'), gen_random_uuid())
ON CONFLICT ("ProviderId") DO NOTHING;
