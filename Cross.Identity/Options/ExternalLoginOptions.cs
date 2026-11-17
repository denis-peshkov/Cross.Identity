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
    /// <summary>Configuration section path: <c>Authentication:ExternalLogin</c>.</summary>
    public const string SectionName = "Authentication:ExternalLogin";

    /// <summary>
    /// OAuth <c>redirect_uri</c> for GET query providers (Google / Microsoft / GitHub / Facebook).
    /// SPA / front-end route (often under <c>Authentication:ClientUrl</c>). Required for those providers.
    /// Not used for Apple (see <see cref="FormCallbackUrl"/>).
    /// Env: <c>AUTH_EXTERNAL_LOGIN_CALLBACK_URL</c> → <c>Authentication__ExternalLogin__CallbackUrl</c>.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Required Apple Sign In <c>redirect_uri</c> (<c>response_mode=form_post</c>) — API / back-end host.
    /// Must accept <b>POST</b> form fields <c>code</c> / <c>state</c> / <c>user</c>
    /// (bridge into <c>main.ExternalLoginCallback</c> — see Sample.Api <c>POST /oauth/callback</c>).
    /// Used for authorize + token exchange; Apple does not use <see cref="CallbackUrl"/>.
    /// Env: <c>AUTH_EXTERNAL_LOGIN_FORM_CALLBACK_URL</c> → <c>Authentication__ExternalLogin__FormCallbackUrl</c>.
    /// </summary>
    public string FormCallbackUrl { get; set; } = string.Empty;

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

/// <summary>
/// Per-provider OAuth client credentials under <see cref="ExternalLoginOptions.Providers"/>.
/// </summary>
public sealed class ExternalLoginProviderOptions
{
    /// <summary>OAuth client id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret.
    /// For Apple Sign In this is the generated client-secret JWT (ES256), not a static password.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// When <c>false</c>, the provider is ignored even if credentials are present.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// <c>true</c> when the provider is enabled and both client id and secret are non-empty.
    /// Used when listing providers for <c>ExternalLoginGetAll</c>.
    /// </summary>
    public bool IsConfigured =>
        IsEnabled
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
