namespace Cross.Identity.Services;

/// <summary>
/// Issues, validates, and revokes JWT access/refresh tokens and related session state in storage.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Access token lifetime in seconds.
    /// </summary>
    int AccessTokenExpiresInSeconds { get; }

    /// <summary>
    /// Issue an <c>id_token</c> (OIDC-like token) from a set of claims.
    /// Synchronous: builds and signs the JWT in memory (no I/O).
    /// </summary>
    /// <param name="claims">Claims to include in the token.</param>
    /// <returns>Token string in compact form.</returns>
    string GenerateIdToken(
        List<Claim> claims);

    /// <summary>
    /// Issue an access token (JWT) for API authorization and persist its <c>jti</c> in storage.
    /// When encryption is enabled, the token is issued as JWE.
    /// </summary>
    /// <param name="userAccountId">User ID.</param>
    /// <param name="familyId">Family/context ID.</param>
    /// <param name="permissions">Permissions to add as claims.</param>
    /// <param name="claims">Additional token claims.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token string in compact form.</returns>
    Task<string> GenerateAccessTokenAsync(
        Guid userAccountId,
        Guid familyId,
        List<string> permissions,
        List<Claim> claims,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Issue a refresh token (JWT) for session rotation and persist its hash in storage.
    /// </summary>
    /// <param name="userAccountId">User ID.</param>
    /// <param name="familyId">Family/context ID.</param>
    /// <param name="claims">Additional refresh-token claims.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh token string.</returns>
    Task<string> GenerateRefreshTokenAsync(
        Guid userAccountId,
        Guid familyId,
        List<Claim> claims,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Cryptographically validate an access token (signature, issuer, audience, lifetime;
    /// decrypts JWE when encryption is enabled), then confirm <c>jti</c> is active in storage
    /// and <c>security_stamp</c> matches <c>UserAccount.SecurityStamp</c> when the account has one.
    /// </summary>
    /// <param name="accessToken">Access token string (JWT/JWE) in compact form.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if crypto checks, DB status, and security stamp all succeed; otherwise <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validate an access token by <c>jti</c> without re-parsing/decrypting the token.
    /// Used in <c>JwtBearerEvents.OnTokenValidated</c> when middleware has already extracted claims.
    /// </summary>
    /// <param name="jti">JTI (access-token identifier) extracted from JWT claims.</param>
    /// <param name="securityStamp">
    /// Stamp from the access-token claim (<see cref="ClaimConstants.SecurityStamp"/>).
    /// When the account has a stamp, this must match; <c>null</c> fails if the account stamp is set.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the <c>jti</c> row is active, the user is active, and the security stamp matches
    /// (when the account has one); otherwise <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenJtiAsync(
        Guid jti,
        Guid? securityStamp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validate a refresh token by its string value.
    /// Expects the refresh token was issued by the server and exists in the refresh-tokens table.
    /// Also requires <c>security_stamp</c> in the JWT to match the account when a stamp is set.
    /// </summary>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the token is valid (not revoked, not expired, stamp ok); otherwise <c>false</c>.</returns>
    Task<bool> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensures the refresh token is present, valid, and issued to <paramref name="userAccountId"/>
    /// (session proof for user-scoped flow operations).
    /// </summary>
    /// <param name="refreshToken">Refresh token string from the authenticated session.</param>
    /// <param name="userAccountId">User account id the caller claims to act on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">
    /// Token is missing, invalid, expired, or belongs to another user.
    /// </exception>
    Task EnsureRefreshTokenBelongsToUserAsync(
        string? refreshToken,
        Guid userAccountId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ensure a refresh token may be used for rotation (exists, not revoked, not expired).
    /// </summary>
    /// <remarks>
    /// If the token exists but is already revoked, this is treated as refresh-token reuse:
    /// the entire family is revoked with <see cref="RefreshTokenRevokedReason.REPLAY_DETECTED"/>
    /// (see that enum for the theft-race rationale), then a conflict is thrown.
    /// When session metadata was captured at family start, refresh compares the current
    /// <see cref="HostSuppliedClientContext"/> (host-supplied <c>collectForm</c> fields) with the family anchor.
    /// Mismatch revokes the family with <see cref="RefreshTokenRevokedReason.DEVICE_MISMATCH"/>,
    /// <see cref="RefreshTokenRevokedReason.IP_MISMATCH"/>,
    /// <see cref="RefreshTokenRevokedReason.USER_AGENT_MISMATCH"/>, or
    /// <see cref="RefreshTokenRevokedReason.TOKEN_STOLEN"/> when multiple dimensions differ.
    /// When <c>Authentication:Jwt:RefreshTokenIdleTimeout</c> is set, refresh also fails with
    /// <see cref="RefreshTokenRevokedReason.SESSION_EXPIRED"/> if <c>LastActivityAt</c> is older than the idle window.
    /// </remarks>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">Token is missing, expired, idle timeout exceeded, or session binding failed.</exception>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    Task EnsureRefreshTokenActiveForRotationAsync(
        string refreshToken,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revoke an access token by <c>jti</c> (mark as revoked in the DB).
    /// </summary>
    /// <param name="jti">JTI (identifier) of the access token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAccessTokenAsync(
        Guid jti,
        CancellationToken cancellationToken);

    /// <summary>
    /// Remove expired access tokens from storage by <c>ExpiresAt</c> (run periodically).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupExpiredAccessTokensAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete refresh tokens whose chain absolute lifetime (<c>AbsoluteExpiresAt</c>) has expired.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CleanupExpiredRefreshTokensAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Get a claim value from a compact JWT/JWE by type(s). Synchronous.
    /// JWS (3 parts): reads the Base64URL JSON payload without verifying the signature.
    /// JWE (5 parts): decrypts and validates, then reads claims from the validated identity.
    /// </summary>
    /// <param name="token">JWT/JWE in compact form.</param>
    /// <param name="claimTypes">
    /// Claim types to search. Matching values overwrite; the last match is returned.
    /// </param>
    /// <returns>Claim value, or <c>null</c> if not found / JWE validation fails.</returns>
    string? GetClaimValue(
        string token,
        params string[] claimTypes);

    /// <summary>
    /// Get a refresh token from storage by its string value.
    /// Uses a token hash internally so the raw token is not stored in plain text.
    /// </summary>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh token entity, or <c>null</c> if not found.</returns>
    Task<RefreshTokenEntity?> GetRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Invalidate (mark as replaced/revoked) a refresh token
    /// during session rotation.
    /// </summary>
    /// <remarks>
    /// If the token is already revoked (concurrent refresh or replay), the entire family is revoked
    /// with <see cref="RefreshTokenRevokedReason.REPLAY_DETECTED"/> before throwing
    /// <see cref="ConflictException"/>. See that enum for why family revoke is required.
    /// </remarks>
    /// <param name="refreshToken">Current refresh token (string) to revoke.</param>
    /// <param name="newJti">
    /// JTI of the new refresh token that replaces the old one (used for reasons and linkage).
    /// </param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    Task InvalidateRefreshTokenAsync(
        string refreshToken,
        string newJti,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revoke a refresh token on user logout and invalidate access tokens in the same session (family).
    /// </summary>
    /// <param name="refreshToken">Refresh token string (e.g. from an httpOnly cookie).</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenForLogoutAsync(
        string? refreshToken,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Logout from all devices: resolve the user from a live refresh token and revoke every
    /// active access/refresh token for that user with <see cref="RefreshTokenRevokedReason.USER_LOGOUT_ALL"/>.
    /// </summary>
    /// <param name="refreshToken">
    /// Current refresh token proving session ownership. Empty/whitespace is a no-op.
    /// </param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">
    /// Refresh token is missing in storage, revoked, or expired.
    /// </exception>
    Task RevokeAllTokensForLogoutAsync(
        string? refreshToken,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revoke all active access and refresh tokens that share <paramref name="familyId"/>.
    /// Persists changes via <c>SaveChanges</c>.
    /// </summary>
    /// <param name="familyId">Refresh/access token family (rotation chain).</param>
    /// <param name="reason">Revocation reason stored on each active token.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenFamilyAsync(
        Guid familyId,
        RefreshTokenRevokedReason reason,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revoke all active access and refresh tokens for a user (e.g. after password change / security stamp rotation).
    /// Does not call <c>SaveChanges</c> — the caller persists changes on the shared <c>IdentityContext</c>.
    /// </summary>
    /// <param name="userAccountId">User whose sessions must be invalidated.</param>
    /// <param name="reason">Revocation reason stored on each token.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllTokensForUserAsync(
        Guid userAccountId,
        RefreshTokenRevokedReason reason,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);
}

// /// <summary>
// /// JWT issuance service.
// /// </summary>
// public interface IJwtIssuer
// {
//     /// <summary>
//     /// Issue a JWT from a claim set and lifetime.
//     /// </summary>
//     /// <param name="claims">
//     /// Claim map. Values may be a string or a collection of strings.
//     /// Required claims (<c>sub</c>, <c>iat</c>, <c>exp</c>) are added automatically by the implementation
//     /// based on settings and <paramref name="lifetime"/>.
//     /// </param>
//     /// <param name="lifetime">Token lifetime (TTL).</param>
//     /// <returns>Signed JWT in compact serialization.</returns>
//     string Issue(IDictionary<string, object> claims, TimeSpan lifetime);
// }
