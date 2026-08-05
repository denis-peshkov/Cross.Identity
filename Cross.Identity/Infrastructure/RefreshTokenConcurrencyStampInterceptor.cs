namespace Cross.Identity.Infrastructure;

/// <summary>
/// Ensures <see cref="Cross.Identity.Entities.RefreshTokenEntity.ConcurrencyStamp"/> is populated/rotated
/// on every insert/update performed via <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
/// <para>
/// Note: bulk updates like <c>ExecuteUpdateAsync</c> bypass <c>SaveChanges</c>, so those code paths must
/// set <c>ConcurrencyStamp</c> explicitly.
/// </para>
/// </summary>
public sealed class RefreshTokenConcurrencyStampInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<RefreshTokenEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.ConcurrencyStamp = Guid.NewGuid();
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
