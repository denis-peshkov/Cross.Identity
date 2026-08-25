namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Normalized profile returned by an external OAuth provider after token exchange.
/// Passed to <see cref="IExternalLoginUserProvisioner.ProvisionAsync"/> when the host registers a provisioner.
/// </summary>
public sealed class ExternalOAuthProfile
{
    /// <summary>Stable subject / user id at the identity provider.</summary>
    public string ProviderUserId { get; init; }

    /// <summary>Email from the provider, if any.</summary>
    public string? Email { get; init; }

    /// <summary>
    /// Whether the provider attests that <see cref="Email"/> is verified at the identity provider.
    /// Maps onto <c>UsersAccounts.EmailVerified</c> when creating a local account.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>Display name from the provider profile, if any.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Avatar URL from the provider profile, if any.</summary>
    public string? AvatarUrl { get; init; }
}
