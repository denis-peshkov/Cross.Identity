namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Result of exchanging an authorization <c>code</c> at the provider token endpoint.
/// </summary>
internal sealed class ExternalOAuthTokenResponse
{
    /// <summary>OAuth access token (Bearer).</summary>
    public required string AccessToken { get; init; }

    /// <summary>
    /// OIDC <c>id_token</c> when returned (required for Apple Sign In profile claims).
    /// </summary>
    public string? IdToken { get; init; }
}
