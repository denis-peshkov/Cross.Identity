namespace Cross.Identity.UnitTests.Core;

public static class InMemoryDbHelper
{
    public static IdentityContext CreateContext(string? dbName = null)
    {
        dbName ??= $"testdb-{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new IdentityContext(options);
        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };
        context.HeadersContextAccessor = headersContextAccessor;
        context.DbContextListener = new NullDbContextListener();
        context.Database.EnsureCreated();
        return context;
    }
}

internal class NullDbContextListener : IDbContextListener
{
    public Task RemoveNonActualCaches(IReadOnlyCollection<string> changedTables)
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<TEntity>> GetAsync<TEntity>(
        string[] tablesNames,
        IQueryable<TEntity> query)
    {
        return Task.FromResult<IReadOnlyCollection<TEntity>>((IReadOnlyCollection<TEntity>) Array.Empty<TEntity>());
    }
}
