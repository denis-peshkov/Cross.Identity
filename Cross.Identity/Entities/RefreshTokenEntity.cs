namespace Cross.Identity.Entities;

public class RefreshTokenEntity : IHasConcurrencyStamp
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
    public RefreshTokenRevokedReason? RevokedReason { get; set; }
    public string? RevokedIpAddress { get; set; }
    public string? RevokedUserAgent { get; set; }

    public string? CreatedDeviceFingerprint { get; set; }
    public string? CreatedUserAgent { get; set; }
    public string? CreatedIpAddress { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}

// ReplacedByTokenId
// IdleTimeout
