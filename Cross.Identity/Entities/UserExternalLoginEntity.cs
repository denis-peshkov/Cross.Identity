namespace Cross.Identity.Entities;

public class UserExternalLoginEntity
{
    public long Id { get; set; }

    public Guid UserAccountId { get; set; }

    public UserAccountEntity UserAccount { get; set; } = null!;

    public short ProviderId { get; set; }

    public ProviderEntity ProviderEntity { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    public string? DisplayName { get; set; }

    public string? AvatarUrl { get; set; }

    public string? ProfileUrl { get; set; }

    public byte[]? AccessTokenEnc { get; set; }

    public byte[]? RefreshTokenEnc { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string? Scope { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsedAt { get; set; }
}
