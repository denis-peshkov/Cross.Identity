namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// External OAuth login service: start authorization, complete callback, and unlink a provider.
/// Used by process steps:
/// <list type="bullet">
/// <item><description><c>ExternalLoginInitiateStep</c> — <see cref="InitiateAsync"/></description></item>
/// <item><description><c>ExternalLoginCompleteStep</c> — <see cref="CompleteAsync"/></description></item>
/// <item><description><c>ExternalLoginUnlinkStep</c> — <see cref="UnlinkAsync"/></description></item>
/// <item><description><c>ExternalLoginGetAllStep</c> — <see cref="GetAllAsync"/></description></item>
/// </list>
/// Corresponding flows:
/// <c>main.ExternalLogin</c>,
/// <c>main.ExternalLoginCallback</c>,
/// <c>main.ExternalLoginUnlink</c>,
/// <c>main.ExternalLoginGetAll</c>.
/// OAuth state is persisted in <c>auth.ExternalLoginStates</c> (nonce + TTL) and consumed once on callback.
/// </summary>
internal interface IExternalLoginService
{
    /// <summary>
    /// Start an OAuth authorization redirect for the given provider.
    /// Creates a one-time state row and returns the provider authorization URL.
    /// </summary>
    /// <param name="provider">
    /// Provider name as registered in options and <c>auth.Providers</c>
    /// (for example, <c>Google</c>, <c>Microsoft</c>, <c>GitHub</c>, <c>Apple</c>).
    /// </param>
    /// <param name="returnUrl">
    /// Optional URL to return to after callback (stored in OAuth state).
    /// When the path contains <c>ExternalLogins</c>, the callback is treated as an account-link flow
    /// even if <paramref name="linkUserId"/> is omitted.
    /// </param>
    /// <param name="linkUserId">
    /// When set, starts an account-link flow for that user.
    /// Requires an authenticated principal whose <c>sub</c> / <c>NameIdentifier</c> matches this id,
    /// and the provider must not already be linked to the user.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full provider authorization URL including opaque <c>state</c>.</returns>
    /// <exception cref="NotFoundException">
    /// Provider is not supported by the built-in catalog, or is not enabled in <c>auth.Providers</c>.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Provider credentials are missing/disabled in options, or the provider is already linked when linking.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// <c>Authentication:ExternalLogin:CallbackUrl</c> is not configured.
    /// </exception>
    /// <exception cref="NotAuthorizedException">
    /// Linking was requested but the caller is not authenticated, or <paramref name="linkUserId"/> does not match the principal.
    /// </exception>
    Task<string> InitiateAsync(
        string provider,
        string? returnUrl,
        Guid? linkUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Complete the OAuth callback: validate and consume state, exchange <paramref name="code"/> for a provider access token,
    /// resolve or create the local user, upsert <c>auth.UsersExternalLogins</c>, and optionally run
    /// <see cref="IExternalLoginUserProvisioner"/>.
    /// </summary>
    /// <param name="code">
    /// Authorization code from the provider.
    /// May be empty when the provider returned an OAuth error redirect; then <paramref name="error"/> is expected.
    /// </param>
    /// <param name="state">
    /// Opaque state returned by <see cref="InitiateAsync"/> (Base64Url-encoded JSON with nonce).
    /// Must match a non-expired, unused row in <c>ExternalLoginStates</c>; the row is deleted (one-time consume).
    /// </param>
    /// <param name="error">
    /// OAuth error code from the provider (for example, <c>access_denied</c>), if any.
    /// </param>
    /// <param name="errorDescription">
    /// Human-readable OAuth error description from the provider, if any.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Local user id and whether the flow was account linking
    /// (<see cref="ExternalLoginCompletion.IsLinking"/> — the step should skip issuing a new token pair).
    /// </returns>
    /// <exception cref="ValidationException">
    /// Provider returned an error; state is invalid, expired, or already used; provider is misconfigured;
    /// the external account is already linked to another user; or the same provider is already linked to this user.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Provider is not supported or not enabled.
    /// </exception>
    /// <exception cref="NotAuthorizedException">
    /// Linking requires authentication and a matching <c>LinkUserId</c>.
    /// </exception>
    Task<ExternalLoginCompletion> CompleteAsync(
        string code,
        string state,
        string? error,
        string? errorDescription,
        CancellationToken cancellationToken);

    /// <summary>
    /// Unlink an external provider from the authenticated user.
    /// Rotates <c>UserAccount.SecurityStamp</c> and revokes all active access/refresh tokens
    /// with <see cref="RefreshTokenRevokeReason.EXTERNAL_LOGIN_REMOVED"/>.
    /// </summary>
    /// <param name="provider">
    /// Provider name to unlink (for example, <c>Google</c>). Must be enabled and currently linked to the caller.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">
    /// Caller is not authenticated.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Provider is not enabled, or is not linked to the current user.
    /// </exception>
    /// <exception cref="ValidationException">
    /// Unlinking would leave the user with no login method (no password and no other external logins).
    /// </exception>
    Task UnlinkAsync(
        string provider,
        CancellationToken cancellationToken);

    /// <summary>
    /// List enabled providers and link status for the authenticated user.
    /// Includes a provider when it is linked, or when credentials are configured in
    /// <see cref="ExternalLoginOptions"/> (<see cref="ExternalLoginProviderOptions.IsConfigured"/>).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Account email and provider rows for the current principal.</returns>
    /// <exception cref="NotAuthorizedException">
    /// Caller is not authenticated.
    /// </exception>
    /// <exception cref="NotFoundException">
    /// Current user account was not found.
    /// </exception>
    Task<ExternalLoginOverviewDto> GetAllAsync(
        CancellationToken cancellationToken);
}
