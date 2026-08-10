INSERT INTO auth."Providers" ("ProviderId", "Name", "Scheme", "IsEnabled", "CreatedAt")
VALUES
    (1, 'Google', 'google', TRUE, (NOW() AT TIME ZONE 'utc')),
    (2, 'Apple', 'apple', TRUE, (NOW() AT TIME ZONE 'utc')),
    (3, 'Microsoft', 'microsoft', TRUE, (NOW() AT TIME ZONE 'utc')),
    (4, 'GitHub', 'github', TRUE, (NOW() AT TIME ZONE 'utc'))
ON CONFLICT ("ProviderId") DO NOTHING;
