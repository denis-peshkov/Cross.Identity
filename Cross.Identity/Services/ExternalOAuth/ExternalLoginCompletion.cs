namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Result of <see cref="IExternalLoginService.CompleteAsync"/>.
/// </summary>
/// <param name="UserId">Local user account id resolved or created during the OAuth callback.</param>
/// <param name="IsLinking">
/// <c>true</c> when the flow linked a provider to an existing authenticated account
/// (no new access/refresh token pair should be issued by the step);
/// <c>false</c> for a normal sign-in that should issue tokens.
/// </param>
internal sealed record ExternalLoginCompletion(Guid UserId, bool IsLinking);
