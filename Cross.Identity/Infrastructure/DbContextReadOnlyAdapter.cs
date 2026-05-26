namespace Cross.Identity.Infrastructure;

public class DbContextReadOnlyAdapter : IReadOnlyDbContext
{
    private readonly IdentityContext _dbContext;

    public DbContextReadOnlyAdapter(IdentityContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IQueryable<T> Query<T>(bool trackChanges = false) where T : class
    {
        var query = _dbContext.Set<T>().AsQueryable();
        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}
