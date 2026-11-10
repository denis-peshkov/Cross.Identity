namespace Cross.Identity.Services.ExternalOAuth;

internal sealed class ExternalOAuthProviderDefinition
{
    public required string Scheme { get; init; }

    public required string AuthorizationEndpoint { get; init; }

    public required string TokenEndpoint { get; init; }

    public required string Scope { get; init; }

    /// <summary>
    /// Loads the provider profile. <paramref name="clientSecret"/> is the configured app secret
    /// (used e.g. for Facebook <c>appsecret_proof</c>); providers that do not need it ignore it.
    /// </summary>
    public required Func<HttpClient, ExternalOAuthTokenResponse, string, CancellationToken, Task<ExternalOAuthProfile>> FetchProfileAsync { get; init; }
}
