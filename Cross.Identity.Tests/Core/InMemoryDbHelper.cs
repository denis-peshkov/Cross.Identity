namespace Cross.Identity.Tests.Core;

public static class InMemoryDbHelper
{
    public static IdentityContext CreateContext(string? dbName = null)
    {
        dbName ??= $"testdb-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableSensitiveDataLogging()
            .AddInterceptors(new ConcurrencyStampInterceptor())
            .Options;

        var context = new IdentityContext(options);
        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };
        context.Database.EnsureCreated();
        return context;
    }
}
