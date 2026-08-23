namespace Cross.Identity.Dtos;

/// <summary>
/// Result of <c>ExternalLoginGetAll</c>: account email and provider link status for the current user.
/// </summary>
public sealed class ExternalLoginOverviewDto
{
    public string? AccountEmail { get; init; }

    public IReadOnlyList<ExternalLoginProviderItemDto> Providers { get; init; } =
        Array.Empty<ExternalLoginProviderItemDto>();
}

/// <summary>
/// One OAuth provider row for the external-logins overview.
/// </summary>
public sealed class ExternalLoginProviderItemDto
{
    public required string Provider { get; init; }

    public required string DisplayName { get; init; }

    public bool IsConnected { get; init; }

    public string? ProviderEmail { get; init; }

    public string? AvatarUrl { get; init; }
}
