namespace Cross.Identity.Entities;

/// <summary>
/// Model for storing access tokens
/// </summary>
public class AccessTokenEntity
{
    /// <summary>Jti</summary>
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }
    public RefreshTokenRevokeReason? RevokeReason { get; set; }
    public string? RevokedByIp { get; set; }

    public string? DeviceFingerprint { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}
