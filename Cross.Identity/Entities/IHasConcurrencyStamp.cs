namespace Cross.Identity.Entities;

/// <summary>
/// Optimistic concurrency token rotated on every insert/update via
/// <see cref="Infrastructure.IdentityContext.SaveChanges(bool)"/> /
/// <see cref="Infrastructure.IdentityContext.SaveChangesAsync(bool, CancellationToken)"/>.
/// </summary>
internal interface IHasConcurrencyStamp
{
    /// <summary>
    /// App-managed optimistic concurrency token.
    /// EF checks the original value on UPDATE/DELETE; <see cref="Infrastructure.IdentityContext"/>
    /// updates it on every tracked insert/update through <c>SaveChanges</c>.
    /// </summary>
    Guid ConcurrencyStamp { get; set; }
}
