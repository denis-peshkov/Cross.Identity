namespace Cross.Identity.Entities;

public class UserExternalLoginEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public required short ProviderId { get; set; }
    public virtual required ProviderEntity ProviderEntity { get; set; }

    public required string ProviderUserId { get; set; }
    public string? ProviderEmail { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ProfileUrl { get; set; }
    public byte[]? AccessTokenEnc { get; set; }
    public byte[]? RefreshTokenEnc { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Scope { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
