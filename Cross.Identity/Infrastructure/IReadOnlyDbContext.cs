namespace Cross.Identity.Infrastructure;

public interface IReadOnlyDbContext
{
    IQueryable<T> Query<T>(bool trackChanges = false) where T : class;
}
