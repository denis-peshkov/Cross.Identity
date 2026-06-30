namespace Cross.Identity.Options;

/// <summary>
/// External OAuth settings (<c>Authentication:ExternalLogin</c>).
/// <para>
/// <c>StateLifetime</c> sets the TTL for rows in <c>auth.ExternalLoginStates</c>
/// (<see cref="Entities.ExternalLoginStateEntity"/>), written by <c>ExternalLoginService</c>:
/// <c>InitiateAsync</c> — insert; <c>ResolveStateAsync</c> (from <c>CompleteAsync</c>) — select and delete.
/// </para>
/// </summary>
public sealed class ExternalLoginOptions
{
    public const string SectionName = "Authentication:ExternalLogin";

    /// <summary>
    /// Full SPA callback URL registered with the OAuth provider.
    /// Env: <c>AUTH_EXTERNAL_LOGIN_CALLBACK_URL</c> → <c>Authentication__ExternalLogin__CallbackUrl</c>.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// OAuth state lifetime in the DB (<c>ExternalLoginStates.ExpiresAt</c>).
    /// </summary>
    public TimeSpan StateLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Provider credentials — from env / user-secrets only, not appsettings.
    /// Example: <c>Authentication__ExternalLogin__Providers__Google__ClientId</c>.
    /// </summary>
    public Dictionary<string, ExternalLoginProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ExternalLoginProviderOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public bool IsConfigured =>
        IsEnabled
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
