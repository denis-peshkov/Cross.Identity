namespace Cross.Identity.Infrastructure;

public class DbContextReadOnlyAdapter : IReadOnlyDbContext
{
    private readonly DbContext _dbContext;

    public DbContextReadOnlyAdapter(DbContext dbContext)
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
