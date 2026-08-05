namespace Cross.Identity.Infrastructure;

/// <summary>
/// Rotates <c>ConcurrencyStamp</c> on insert/update via <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// for <see cref="RefreshTokenEntity"/> and <see cref="UserAccountEntity"/>.
/// <para>
/// Bulk updates like <c>ExecuteUpdateAsync</c> bypass <c>SaveChanges</c>; those paths must set
/// <c>ConcurrencyStamp</c> explicitly (see <see cref="JwtTokenService"/>).
/// </para>
/// </summary>
public sealed class ConcurrencyStampInterceptor : SaveChangesInterceptor
{
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

        foreach (var entry in dbContext.ChangeTracker.Entries<RefreshTokenEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.ConcurrencyStamp = Guid.NewGuid();
            }
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<UserAccountEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.ConcurrencyStamp = Guid.NewGuid();
            }
        }
    }
}
