namespace Cross.Identity.Services.ExternalOAuth;

public sealed class ExternalOAuthProfile
{
    public string ProviderUserId { get; init; }

    public string? Email { get; init; }

    public string? DisplayName { get; init; }

    public string? AvatarUrl { get; init; }
}
