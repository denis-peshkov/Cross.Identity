namespace Cross.Identity.Dtos;

/// <summary>
/// Result of <c>ExternalLoginGetAll</c>: account email and provider link status for the current user.
/// Host must authorize <c>UserAccountId</c> before running the flow or calling the service.
/// </summary>
public sealed class ExternalLoginOverviewDto
{
    /// <summary>Local account email from <c>UsersAccounts</c>, if any.</summary>
    public string? AccountEmail { get; init; }

    /// <summary>
    /// Providers that are linked and/or have credentials configured in
    /// <see cref="Options.ExternalLoginOptions"/>.
    /// </summary>
    public IReadOnlyList<ExternalLoginProviderItemDto> Providers { get; init; } =
        Array.Empty<ExternalLoginProviderItemDto>();
}

/// <summary>
/// One OAuth provider row for the external-logins overview.
/// </summary>
public sealed class ExternalLoginProviderItemDto
{
    /// <summary>Provider key (for example <c>Google</c>, <c>Microsoft</c>).</summary>
    public required string Provider { get; init; }

    /// <summary>Human-readable provider label for UI.</summary>
    public required string DisplayName { get; init; }

    /// <summary><c>true</c> when this provider is linked to the user in <c>UsersExternalLogins</c>.</summary>
    public bool IsConnected { get; init; }

    /// <summary>Email reported by the provider for the linked account, if known.</summary>
    public string? ProviderEmail { get; init; }

    /// <summary>Avatar URL from the provider profile, if known.</summary>
    public string? AvatarUrl { get; init; }
}
