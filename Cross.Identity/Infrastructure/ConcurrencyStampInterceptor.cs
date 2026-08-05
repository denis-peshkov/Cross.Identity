namespace Cross.Identity.Infrastructure;

/// <summary>
/// Rotates <see cref="IHasConcurrencyStamp.ConcurrencyStamp"/> on insert/update via
/// <see cref="DbContext.SaveChangesAsync(CancellationToken)"/> for all identity entities that implement the interface.
/// <para>
/// Bulk updates like <c>ExecuteUpdateAsync</c> bypass <c>SaveChanges</c>; those paths must set
/// <c>ConcurrencyStamp</c> explicitly.
/// </para>
/// </summary>
public sealed class ConcurrencyStampInterceptor : SaveChangesInterceptor
{
    /// <summary>Shared instance used by <see cref="IdentityContext"/> (safe to reuse; interceptor is stateless).</summary>
    public static ConcurrencyStampInterceptor Instance { get; } = new();

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        RotateConcurrencyStamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        RotateConcurrencyStamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void RotateConcurrencyStamps(DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            if (entry.Entity is IHasConcurrencyStamp stamped)
            {
                stamped.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }
}
