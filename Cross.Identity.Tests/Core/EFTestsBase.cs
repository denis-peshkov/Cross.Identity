namespace Cross.Identity.Tests.Core;

/// <summary>
/// Base for tests with a real <see cref="IdentityContext"/> (InMemory) and persistence.
/// </summary>
[TestFixture]
public class EFTestsBase
{
    protected IdentityContext Context;
    private string _dbName = null!;

    /// <summary>InMemory database name shared by <see cref="Context"/> and factory-created contexts.</summary>
    protected string DatabaseName => _dbName;

    [SetUp]
    public virtual void Setup()
    {
        _dbName = $"testdb-{Guid.NewGuid()}";
        Context = InMemoryDbHelper.CreateContext(_dbName);
    }

    [TearDown]
    public virtual void TearDown()
    {
        Context?.Dispose();
    }

    /// <summary>
    /// Factory that opens a new <see cref="IdentityContext"/> against the same InMemory store as <see cref="Context"/>.
    /// </summary>
    protected IDbContextFactory<IdentityContext> CreateIdentityContextFactory()
        => new SharingInMemoryIdentityContextFactory(_dbName);

    protected void AddToDb<TEntity>(params TEntity[] entities)
        where TEntity : class
    {
        if (entities.Length == 0)
        {
            return;
        }

        Context.Set<TEntity>().AddRange(entities);
        Context.SaveChanges();
    }

    protected void AddToDb<TEntity>(List<TEntity> entities)
        where TEntity : class
    {
        if (entities.Count == 0)
        {
            return;
        }

        Context.Set<TEntity>().AddRange(entities);
        Context.SaveChanges();
    }
}
