namespace Cross.Identity.Services.ExternalOAuth;

internal sealed class ExternalOAuthProviderDefinition
{
    public required string Scheme { get; init; }

    public required string AuthorizationEndpoint { get; init; }

    public required string TokenEndpoint { get; init; }

    public required string Scope { get; init; }

    public required Func<HttpClient, string, CancellationToken, Task<ExternalOAuthProfile>> FetchProfileAsync { get; init; }
}
