namespace Cross.Identity.Entities;

/// <summary>
/// Append-only audit record for identity / security operations.
/// </summary>
public class AuditEntity
{
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    /// <summary>What happened.</summary>
    public AuditOperation Operation { get; set; }
    /// <summary>Logical entity kind the operation targeted.</summary>
    public AuditEntityType EntityType { get; set; }
    /// <summary>Identifier of the target entity as a string (Guid, etc.).</summary>
    public string? EntityId { get; set; }

    /// <summary>Optional token-revoke reason when <see cref="Operation"/> is revoke-related.</summary>
    public RefreshTokenRevokedReason? RevokedReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceFingerprint { get; set; }

    /// <summary>Free-form details for operators / debugging.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
}
