namespace Cross.Identity.Entities;

/// <summary>
/// One-time pending merge token after an identity conflict (OAuth link, and later email/phone).
/// Bound to survivor A and absorbed B. History of the completed merge lives in <c>Audits</c>
/// (<see cref="Enums.AuditOperation.UserAccountsMerged"/>), not on this row.
/// </summary>
public class AccountMergePendingEntity : IHasConcurrencyStamp
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Opaque one-time token returned to the client on conflict.</summary>
    public string MergeToken { get; set; } = null!;

    /// <summary>Survivor account (current session) — A.</summary>
    public Guid SurvivorUserAccountId { get; set; }

    /// <summary>Survivor navigation.</summary>
    public virtual UserAccountEntity? SurvivorUserAccount { get; set; }

    /// <summary>Absorbed account — B.</summary>
    public Guid AbsorbedUserAccountId { get; set; }

    /// <summary>Absorbed navigation.</summary>
    public virtual UserAccountEntity? AbsorbedUserAccount { get; set; }

    /// <summary>UTC expiry; rejected after this instant.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC creation time.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC consume time; set when merge succeeds.</summary>
    public DateTime? ConsumedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
