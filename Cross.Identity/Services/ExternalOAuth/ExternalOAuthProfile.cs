namespace Cross.Identity.Services.ExternalOAuth;

public sealed class ExternalOAuthProfile
{
    public string ProviderUserId { get; init; }

    public string? Email { get; init; }

    /// <summary>
    /// Whether the provider attests that <see cref="Email"/> is verified at the identity provider.
    /// </summary>
    public bool EmailVerified { get; init; }

    public string? DisplayName { get; init; }

    public string? AvatarUrl { get; init; }
}
