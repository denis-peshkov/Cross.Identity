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
    /// Ensure a refresh token row may be used for rotation (exists, not revoked, not expired).
    /// </summary>
    /// <remarks>
    /// If the row exists but is already revoked, this is treated as refresh-token reuse:
    /// the entire family is revoked with <see cref="RefreshTokenRevokedReason.REPLAY_DETECTED"/>
    /// (see that enum for the theft-race rationale), then a conflict is thrown.
    /// When session metadata was captured at family start, refresh compares the current
    /// <see cref="HostSuppliedClientContext"/> (host-supplied <c>collectForm</c> fields) with the family anchor.
    /// When <c>Authentication:Jwt:SessionBindingCheckIp</c> is <c>true</c> and the anchor has binding data,
    /// <paramref name="hostSuppliedClientContext"/> must not be <see cref="HostSuppliedClientContext.Empty"/> —
    /// pass the same trusted server-side metadata as on Token (otherwise <see cref="ValidationException"/>).
    /// Mismatch revokes the family with <see cref="RefreshTokenRevokedReason.DEVICE_MISMATCH"/>,
    /// <see cref="RefreshTokenRevokedReason.USER_AGENT_MISMATCH"/>, or
    /// <see cref="RefreshTokenRevokedReason.TOKEN_STOLEN"/> when multiple dimensions differ.
    /// IP is compared only when <c>Authentication:Jwt:SessionBindingCheckIp</c> is <c>true</c>
    /// (<see cref="RefreshTokenRevokedReason.IP_MISMATCH"/>).
    /// When <c>Authentication:Jwt:RefreshTokenIdleTimeout</c> is set, refresh also fails with
    /// <see cref="RefreshTokenRevokedReason.SESSION_EXPIRED"/> if <c>LastActivityAt</c> is older than the idle window.
    /// </remarks>
    /// <param name="refreshTokenJti"><see cref="RefreshTokenEntity.Id"/> of the refresh token to rotate.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata. On refresh when <c>SessionBindingCheckIp</c> is enabled, pass the same trusted values as at login (not <see cref="HostSuppliedClientContext.Empty"/>). <see cref="HostSuppliedClientContext.Empty"/> is fine on logout/revoke/password APIs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">Token is missing, expired, idle timeout exceeded, or session binding failed.</exception>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    /// <exception cref="ValidationException"><paramref name="hostSuppliedClientContext"/> is <see cref="HostSuppliedClientContext.Empty"/> while IP session binding is enabled and the family anchor captured metadata.</exception>
    Task EnsureRefreshTokenActiveForRotationAsync(
        Guid refreshTokenJti,
        HostSuppliedClientContext hostSuppliedClientContext,
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
    /// Get a refresh token row by <c>RefreshTokens.Id</c> (<c>jti</c>).
    /// </summary>
    /// <param name="refreshTokenJti">Refresh token JTI.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<RefreshTokenEntity?> GetRefreshTokenByIdAsync(
        Guid refreshTokenJti,
        CancellationToken cancellationToken);

    /// <summary>
    /// Invalidate a refresh token row during rotation (lookup by <c>RefreshTokens.Id</c>).
    /// </summary>
    /// <remarks>
    /// If the token is already revoked (concurrent refresh or replay), the entire family is revoked
    /// with <see cref="RefreshTokenRevokedReason.REPLAY_DETECTED"/> before throwing
    /// <see cref="ConflictException"/>. See that enum for why family revoke is required.
    /// </remarks>
    /// <param name="refreshTokenJti">JTI of the refresh token row being rotated out.</param>
    /// <param name="newRefreshTokenJti">JTI of the new refresh token row.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    Task InvalidateRefreshTokenAsync(
        Guid refreshTokenJti,
        Guid newRefreshTokenJti,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Logout current session: resolve the token family from access-token <c>jti</c> and revoke the whole family
    /// (all active refresh + access tokens) with <see cref="RefreshTokenRevokedReason.USER_LOGOUT"/>.
    /// Missing or already-revoked <paramref name="accessTokenJti"/> is a no-op (idempotent).
    /// </summary>
    /// <param name="accessTokenJti">Access token JTI identifying the session to revoke.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeSessionForLogoutAsync(
        Guid accessTokenJti,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revoke all active access and refresh tokens for a user (e.g. password change, logout-all, admin revoke).
    /// Persists via <c>SaveChanges</c>.
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
