namespace Cross.Identity.Entities;

/// <summary>
/// External OAuth/OIDC identity linked to a local <see cref="UserAccountEntity"/>.
/// </summary>
public class UserExternalLoginEntity : IHasConcurrencyStamp
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner account id.</summary>
    public required Guid UserAccountId { get; set; }

    /// <summary>Owner account navigation.</summary>
    public virtual required UserAccountEntity UserAccount { get; set; }

    /// <summary>FK to <see cref="ProviderEntity"/>.</summary>
    public required short ProviderId { get; set; }

    /// <summary>Provider registry entry.</summary>
    public virtual required ProviderEntity ProviderEntity { get; set; }

    /// <summary>Stable subject/id from the external provider.</summary>
    public required string ProviderUserId { get; set; }

    /// <summary>Email reported by the provider at link time (may differ from account email).</summary>
    public string? ProviderEmail { get; set; }

    /// <summary>Display name from the provider profile.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Avatar URL from the provider profile.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Profile page URL from the provider.</summary>
    public string? ProfileUrl { get; set; }

    /// <summary>Encrypted provider access token (optional).</summary>
    public byte[]? AccessTokenEnc { get; set; }

    /// <summary>Encrypted provider refresh token (optional).</summary>
    public byte[]? RefreshTokenEnc { get; set; }

    /// <summary>UTC expiry of stored provider tokens, when applicable.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>OAuth scopes granted to the stored tokens.</summary>
    public string? Scope { get; set; }

    /// <summary>UTC time the link row was last updated.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>UTC time the link was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
