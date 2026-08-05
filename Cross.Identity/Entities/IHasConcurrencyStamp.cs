namespace Cross.Identity.Entities;

/// <summary>
/// Optimistic concurrency token rotated on every insert/update via <see cref="Infrastructure.ConcurrencyStampInterceptor"/>.
/// </summary>
internal interface IHasConcurrencyStamp
{
    /// <summary>
    /// App-managed optimistic concurrency token.
    /// EF checks the original value on UPDATE/DELETE; the interceptor updates it on every logical mutation.
    /// </summary>
    Guid ConcurrencyStamp { get; set; }
}
