namespace Cross.Identity.Tests.Core;

/// <summary>
/// <see cref="IDbContextFactory{TContext}"/> that creates InMemory contexts sharing one database name.
/// </summary>
internal sealed class SharingInMemoryIdentityContextFactory : IDbContextFactory<IdentityContext>
{
    private readonly string _databaseName;

    public SharingInMemoryIdentityContextFactory(string databaseName)
    {
        _databaseName = databaseName;
    }

    /// <inheritdoc />
    public IdentityContext CreateDbContext()
        => InMemoryDbHelper.CreateContext(_databaseName);
}
