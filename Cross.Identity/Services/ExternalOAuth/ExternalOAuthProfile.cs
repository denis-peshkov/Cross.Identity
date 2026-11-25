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
    /// Whether <see cref="Email"/> is treated as verified for account provisioning.
    /// CompleteAsync requires a non-empty email and forces this flag to <c>true</c>; providers without an email
    /// are rejected. On new local registration via OAuth, copied to <c>UsersAccounts.EmailVerified</c>.
    /// Re-login / link does not clear <c>UsersAccounts.EmailVerified</c>.
    /// Communication endpoint sync for a linked <see cref="Email"/> always sets <c>IsVerified = true</c>.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>Display name from the provider profile, if any.</summary>
    public string? DisplayName { get; init; }

    /// <summary>Avatar URL from the provider profile, if any.</summary>
    public string? AvatarUrl { get; init; }
}
