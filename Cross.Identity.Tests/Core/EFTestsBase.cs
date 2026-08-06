namespace Cross.Identity.Tests.Core;

/// <summary>
/// Base for tests with a real <see cref="IdentityContext"/> (InMemory) and persistence.
/// </summary>
[TestFixture]
public class EFTestsBase
{
    protected IdentityContext Context;
    private string _dbName = null!;

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
