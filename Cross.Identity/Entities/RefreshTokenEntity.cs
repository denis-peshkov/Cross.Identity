namespace Cross.Identity.Entities;

/// <summary>
/// Refresh token in a rotation chain; stores the family anchor for session binding (<c>Created*</c>).
/// </summary>
public class RefreshTokenEntity : IHasConcurrencyStamp
{
    /// <summary>Token id (<c>jti</c>).</summary>
    public Guid Id { get; set; }

    /// <summary>Account the session belongs to.</summary>
    public required Guid UserAccountId { get; set; }

    /// <summary>Account navigation.</summary>
    public virtual required UserAccountEntity UserAccount { get; set; }

    /// <summary>Rotation chain id shared with sibling access tokens.</summary>
    public Guid FamilyId { get; set; }

    /// <summary>Hash of the compact refresh token string (raw token is never stored).</summary>
    public string TokenHash { get; set; }

    /// <summary>UTC sliding expiry of this token row.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC absolute cap for the entire refresh chain.</summary>
    public DateTime AbsoluteExpiresAt { get; set; }

    /// <summary>UTC time this row was issued.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last successful refresh (or login) that issued this token; used for idle timeout.</summary>
    public DateTime LastActivityAt { get; set; }

    /// <summary>Host-supplied IP captured at family start (session binding anchor).</summary>
    public string? CreatedIpAddress { get; set; }

    /// <summary>Host-supplied User-Agent captured at family start (session binding anchor).</summary>
    public string? CreatedUserAgent { get; set; }

    /// <summary>Host-supplied device fingerprint captured at family start (session binding anchor).</summary>
    public string? CreatedDeviceFingerprint { get; set; }

    /// <summary>Jti of the replacement token when this row was rotated out.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    /// <summary>When set, the token is no longer valid. Details go to <see cref="AuditEntity"/>.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
