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
/// </para>
/// <para>
/// Keys:
/// <list type="bullet">
///   <item><description><see cref="RefreshTokenKey"/>:
///     if the key is relative (no dot), it is read as <c>"{Kind}.{Key}"</c>;
///     to read data from another step, specify an absolute key such as <c>"other-step.Field"</c>.</description></item>
///   <item><description>The result is written to keys:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserId</c>
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

    /// <summary>Key in <see cref="Bag"/> to read the client IP from. May be relative or absolute.</summary>
    public required string IpAddressKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the User-Agent from. May be relative or absolute.</summary>
    public required string UserAgentKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the device fingerprint from. May be relative or absolute.</summary>
    public required string DeviceFingerprintKey { get; init; }

    /// <summary>Step logger.</summary>
    public required ILogger Logger { get; init; }

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
        var ipAddress = ctx.Get<string?>(BagKey.Qualify(Kind, IpAddressKey));
        var userAgent = ctx.Get<string?>(BagKey.Qualify(Kind, UserAgentKey));
        var deviceFingerprint = ctx.Get<string?>(BagKey.Qualify(Kind, DeviceFingerprintKey));
        await JwtTokenService.EnsureRefreshTokenActiveForRotationAsync(oldRefreshTokenHashValue, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);

        // 2) get UserId from the refresh token
        var oldRefreshToken = await JwtTokenService.GetRefreshTokenAsync(oldRefreshTokenHashValue, cancellationToken).ConfigureAwait(false);
        if (oldRefreshToken is null)
        {
            throw new InvalidOperationException("User not found when refresh token.");
        }

        // 3) get user data
        var user = (await UserService.GetUserByAsync(selectorField: "Id", selectorValue: oldRefreshToken.UserAccountId.ToString(), cancellationToken).ConfigureAwait(false)).ToBag();
        ArgumentNullException.ThrowIfNull(user);
        var userId = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("Invalid user ID when refresh token.");
        }
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("PhoneNumber", out var phoneObj) ? phoneObj?.ToString() : null;
        var username = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        // 4) generate AccessToken
        var accessClaims = new List<Claim>()
            .AddIfNotNull(JwtRegisteredClaimNames.Sub, userId.ToString())
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);
        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, oldRefreshToken.FamilyId, new List<string>(), accessClaims, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrEmpty(accessToken);

        // 5) generate RefreshToken
        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(userId, oldRefreshToken.FamilyId, new List<Claim>{new (JwtRegisteredClaimNames.Sub, userId.ToString())}, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        // 6) Invalidate old RefreshToken
        var newJti = JwtTokenService.GetClaimValue(refreshToken, JwtRegisteredClaimNames.Jti);
        ArgumentException.ThrowIfNullOrEmpty(newJti);
        await JwtTokenService.InvalidateRefreshTokenAsync(oldRefreshTokenHashValue, newJti, ipAddress, userAgent, deviceFingerprint, cancellationToken).ConfigureAwait(false);

        // 7) store the token in Bag
        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

        return StepResult.Ok(Next);
    }
}
