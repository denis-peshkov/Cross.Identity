namespace Cross.Identity.Entities;

public class RefreshTokenEntity
{
    /// <summary>Jti</summary>
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public DateTime? RevokedAt { get; set; }
    public RefreshTokenRevokeReason? RevokeReason { get; set; }
    public string? RevokedByIp { get; set; }

    public string? DeviceFingerprint { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    /// <summary>
    /// App-managed optimistic concurrency token.
    /// EF checks the original value on UPDATE/DELETE; the code updates it on every logical mutation.
    /// </summary>
    public Guid ConcurrencyStamp { get; set; }
}

// ReplacedByTokenId
// IdleTimeout
