namespace Cross.Identity.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Issue an <c>id_token</c> (OIDC-like token) from a set of claims.
    /// </summary>
    /// <param name="claims">Claims to include in the token.</param>
    /// <returns>Token string in compact form.</returns>
    Task<string> GenerateIdTokenAsync(List<Claim> claims);

    /// <summary>
    /// Issue an access token (JWT) for API authorization.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="familyId">Family/context ID.</param>
    /// <param name="permissions">Permissions to add as claims.</param>
    /// <param name="claims">Additional token claims.</param>
    /// <returns>Access token string in compact form.</returns>
    Task<string> GenerateAccessTokenAsync(Guid userId, Guid familyId, List<string> permissions, List<Claim> claims);

    /// <summary>
    /// Issue a refresh token (JWT) for session rotation.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="familyId">Family/context ID.</param>
    /// <param name="claims">Additional refresh-token claims.</param>
    /// <returns>Refresh token string.</returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, Guid familyId, List<Claim> claims);

    /// <summary>
    /// Validate an access token by <c>jti</c>.
    /// Typically used when the raw JWT string is available and can be parsed safely.
    /// <para>
    /// For encrypted (JWE) tokens, prefer <see cref="ValidateAccessTokenJtiAsync"/>,
    /// because middleware has already extracted claims from the token.
    /// </para>
    /// </summary>
    /// <param name="accessToken">Access token string (JWT/JWE) in compact form.</param>
    /// <returns>
    /// <c>true</c> if the token is considered valid (not revoked and not expired per DB data);
    /// otherwise <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Validate an access token by <c>jti</c> without re-parsing/decrypting the token.
    /// Used in <c>JwtBearerEvents.OnTokenValidated</c> when middleware has already extracted claims.
    /// </summary>
    /// <param name="jti">JTI (access-token identifier) extracted from JWT claims.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the access token with the given <c>jti</c> exists in the DB, is not revoked, and not expired;
    /// otherwise <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenJtiAsync(Guid jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a refresh token by its string value.
    /// Expects the refresh token was issued by the server and exists in the refresh-tokens table.
    /// </summary>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <returns><c>true</c> if the token is valid (not revoked and not expired); otherwise <c>false</c>.</returns>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Ensure a refresh token may be used for rotation (exists, not revoked, not expired).
    /// </summary>
    /// <remarks>
    /// If the token exists but is already revoked, this is treated as refresh-token reuse:
    /// the entire family is revoked with <see cref="RefreshTokenRevokeReason.REPLAY_DETECTED"/>
    /// (see that enum for the theft-race rationale), then a conflict is thrown.
    /// </remarks>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotAuthorizedException">Token is missing or expired.</exception>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    Task EnsureRefreshTokenActiveForRotationAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke an access token by <c>jti</c> (mark as revoked in the DB).
    /// </summary>
    /// <param name="jti">JTI (identifier) of the access token.</param>
    Task RevokeAccessTokenAsync(Guid jti);

    /// <summary>
    /// Remove expired access tokens from storage by <c>ExpiresAt</c> (run periodically).
    /// </summary>
    Task CleanupExpiredAccessTokensAsync();

    /// <summary>
    /// Delete refresh tokens whose chain absolute lifetime (<c>AbsoluteExpiresAt</c>) has expired.
    /// </summary>
    Task CleanupExpiredRefreshTokensAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a claim value from a JWT by type(s).
    /// </summary>
    /// <param name="token">JWT in compact form.</param>
    /// <param name="claimTypes">
    /// Claim types to search. The first matching type is returned as the value.
    /// </param>
    /// <returns>Claim value, or <c>null</c> if not found.</returns>
    Task<string?> GetClaimValueAsync(string token, params string[] claimTypes);

    /// <summary>
    /// Get a refresh token from storage by its string value.
    /// Uses a token hash internally so the raw token is not stored in plain text.
    /// </summary>
    /// <param name="refreshToken">Refresh token string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Refresh token entity, or <c>null</c> if not found.</returns>
    Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Access token lifetime in seconds.
    /// </summary>
    int AccessTokenExpiresInSeconds { get; }

    /// <summary>
    /// Invalidate (mark as replaced/revoked) a refresh token
    /// during session rotation.
    /// </summary>
    /// <remarks>
    /// If the token is already revoked (concurrent refresh or replay), the entire family is revoked
    /// with <see cref="RefreshTokenRevokeReason.REPLAY_DETECTED"/> before throwing
    /// <see cref="ConflictException"/>. See that enum for why family revoke is required.
    /// </remarks>
    /// <param name="refreshToken">Current refresh token (string) to revoke.</param>
    /// <param name="newJti">
    /// JTI of the new refresh token that replaces the old one (used for reasons and linkage).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ConflictException">Token was already used; family revoked with <c>REPLAY_DETECTED</c>.</exception>
    Task InvalidateRefreshTokenAsync(string refreshToken, string newJti, CancellationToken cancellationToken);

    /// <summary>
    /// Revoke a refresh token on user logout: mark in the DB so refresh cannot be reused.
    /// </summary>
    /// <param name="refreshToken">Refresh token string (e.g. from an httpOnly cookie).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenForLogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all active access and refresh tokens that share <paramref name="familyId"/>.
    /// Persists changes via <c>SaveChanges</c>.
    /// </summary>
    /// <param name="familyId">Refresh/access token family (rotation chain).</param>
    /// <param name="reason">Revocation reason stored on each active token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeRefreshTokenFamilyAsync(
        Guid familyId,
        RefreshTokenRevokeReason reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all active access and refresh tokens for a user (e.g. after password change / security stamp rotation).
    /// Does not call <c>SaveChanges</c> — the caller persists changes on the shared <c>IdentityContext</c>.
    /// </summary>
    /// <param name="userId">User whose sessions must be invalidated.</param>
    /// <param name="reason">Revocation reason stored on each token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllTokensForUserAsync(Guid userId, RefreshTokenRevokeReason reason, CancellationToken cancellationToken = default);
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
