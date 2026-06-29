INSERT INTO [auth].[Providers] ([ProviderId], [Name], [Scheme], [IsEnabled], [CreatedAt])
VALUES
    (1, N'Google', N'google', 1, SYSUTCDATETIME()),
    (2, N'Apple', N'apple', 1, SYSUTCDATETIME()),
    (3, N'Microsoft', N'microsoft', 1, SYSUTCDATETIME()),
    (4, N'GitHub', N'github', 1, SYSUTCDATETIME());
GO
