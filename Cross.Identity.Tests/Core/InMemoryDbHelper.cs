namespace Cross.Identity.Tests.Core;

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
        context.Database.EnsureCreated();
        return context;
    }
}
