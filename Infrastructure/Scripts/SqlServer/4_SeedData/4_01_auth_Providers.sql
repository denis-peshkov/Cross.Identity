INSERT INTO [auth].[Providers] ([ProviderId], [Name], [Scheme], [IsEnabled], [CreatedAt], [ConcurrencyStamp])
VALUES
    (1, N'Google', N'google', 1, SYSUTCDATETIME(), NEWID()),
    (2, N'Apple', N'apple', 1, SYSUTCDATETIME(), NEWID()),
    (3, N'Microsoft', N'microsoft', 1, SYSUTCDATETIME(), NEWID()),
    (4, N'GitHub', N'github', 1, SYSUTCDATETIME(), NEWID());
GO
