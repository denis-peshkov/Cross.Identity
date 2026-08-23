namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Refresh token rotation step:
/// validates the incoming refresh token, issues a new token pair, and invalidates the old refresh token.
/// <para>
/// This step does not open a database transaction. The host should wrap the refresh flow
/// (same scoped <see cref="IdentityContext"/>) in an external transaction so validation,
/// new-token persistence, and old-token invalidation commit together.
/// </para>
/// <para>
/// Reuse of an already rotated refresh token triggers family revoke with
/// <see cref="RefreshTokenRevokedReason.REPLAY_DETECTED"/> (theft race: attacker may hold the newer token).
/// See <see cref="IJwtTokenService.EnsureRefreshTokenActiveForRotationAsync"/>.
/// Session binding compares host-supplied <c>collectForm</c> metadata with the family anchor.
/// Idle timeout is enforced when <c>Authentication:Jwt:RefreshTokenIdleTimeout</c> is set.
/// </para>
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="RefreshTokenKey"/>:
///     if the key is relative (no dot), it is read as <c>"{Kind}.{Key}"</c>;
///     to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description>The result is written to keys:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserAccountId</c>
///     (with the <c>{Kind}.</c> prefix for relative access).</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class RefreshTokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the source refresh token from. May be relative or absolute.</summary>
    public required string RefreshTokenKey { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <summary>User read service.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Authentication options.</summary>
    public required AuthenticationOptions AuthenticationOptions { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) validate the token (revoked reuse → family REPLAY_DETECTED + Conflict)
        var oldRefreshTokenHashValue = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);
        await JwtTokenService.EnsureRefreshTokenActiveForRotationAsync(oldRefreshTokenHashValue, hostSuppliedClientContext, cancellationToken).ConfigureAwait(false);

        // 2) get UserId from the refresh token
        var oldRefreshToken = await JwtTokenService.GetRefreshTokenAsync(oldRefreshTokenHashValue, cancellationToken).ConfigureAwait(false);
        if (oldRefreshToken is null)
        {
            throw new InvalidOperationException("User not found when refresh token.");
        }

        // 3) get user data
        var user = await UserService.GetUserByAsync(selectorField: "Id", selectorValue: oldRefreshToken.UserAccountId.ToString(), cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(user);

        // 4–5) issue new pair into bag
        await TokenPairIssuer
            .IssueTokenPairAsync(JwtTokenService, ctx, Kind, user, oldRefreshToken.FamilyId, hostSuppliedClientContext, cancellationToken)
            .ConfigureAwait(false);

        // 6) Invalidate old RefreshToken
        var refreshToken = ctx.Get<string>(BagKey.Qualify(Kind, "RefreshToken"));
        var newJti = JwtTokenService.GetClaimValue(refreshToken, JwtRegisteredClaimNames.Jti);
        ArgumentException.ThrowIfNullOrEmpty(newJti);
        await JwtTokenService.InvalidateRefreshTokenAsync(oldRefreshTokenHashValue, newJti, hostSuppliedClientContext, cancellationToken).ConfigureAwait(false);

        return StepResult.Ok(Next);
    }
}
