namespace Cross.Identity.Services.ExternalOAuth;

public sealed class ExternalOAuthProfile
{
    public string ProviderUserId { get; init; }

    public string? Email { get; init; }

    /// <summary>
    /// Whether the provider attests that <see cref="Email"/> is confirmed at the identity provider.
    /// Maps onto <c>UsersAccounts.EmailConfirmed</c> when creating a local account.
    /// </summary>
    public bool EmailConfirmed { get; init; }

    public string? DisplayName { get; init; }

    public string? AvatarUrl { get; init; }
}
