namespace Cross.Identity.Entities;

public class RefreshTokenEntity : IHasConcurrencyStamp
{
    /// <summary>Jti</summary>
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Trusted client IP captured at family start (see session binding).</summary>
    public string? CreatedIpAddress { get; set; }
    /// <summary>Trusted User-Agent captured at family start (see session binding).</summary>
    public string? CreatedUserAgent { get; set; }
    /// <summary>Trusted device fingerprint captured at family start (see session binding).</summary>
    public string? CreatedDeviceFingerprint { get; set; }

    public Guid? ReplacedByTokenId { get; set; }
    /// <summary>When set, the token is no longer valid. Details go to <see cref="AuditEntity"/>.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}

// ReplacedByTokenId
// IdleTimeout
