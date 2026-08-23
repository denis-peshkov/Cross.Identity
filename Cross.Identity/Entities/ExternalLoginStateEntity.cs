namespace Cross.Identity.Entities;

/// <summary>
/// Short-lived OAuth state row for external login initiate/callback (nonce, provider, optional link target).
/// </summary>
public class ExternalLoginStateEntity : IHasConcurrencyStamp
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Set when linking an external login to an existing account.</summary>
    public Guid? UserAccountId { get; set; }

    /// <summary>Target account when <see cref="UserAccountId"/> is set.</summary>
    public virtual UserAccountEntity? UserAccount { get; set; }

    /// <summary>Random nonce echoed in the OAuth state parameter.</summary>
    public string Nonce { get; set; } = null!;

    /// <summary>Provider name/key (matches <see cref="ProviderEntity"/> registration).</summary>
    public string Provider { get; set; } = null!;

    /// <summary>Host return URL after OAuth completes.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>UTC expiry after which the state row is rejected.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC time the row was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
