namespace Cross.Identity.ProcessEngine.Helpers;

/// <summary>
/// Shared JWT access+refresh issuance for process steps (token / refresh / external login).
/// </summary>
internal static class TokenPairIssuer
{
    /// <summary>
    /// Builds identity claims, issues access + refresh tokens, and writes the pair into the bag
    /// under <paramref name="stepKind"/> (<c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>,
    /// <c>ExpiresIn</c>, <c>UserId</c>).
    /// </summary>
    public static async Task IssueTokenPairAsync(
        IJwtTokenService jwt,
        Bag ctx,
        string stepKind,
        UserAccountEntity user,
        Guid familyId,
        ClientContext client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(jwt);
        ArgumentNullException.ThrowIfNull(ctx);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(stepKind);

        var accessClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            }
            .AddIfNotNull(ClaimTypes.Email, user.Email)
            .AddIfNotNull(ClaimTypes.MobilePhone, user.PhoneNumber)
            .AddIfNotNull(ClaimConstants.Username, user.UserName);

        var accessToken = await jwt
            .GenerateAccessTokenAsync(
                user.Id,
                familyId,
                new List<string>(),
                accessClaims,
                client.IpAddress,
                client.UserAgent,
                client.DeviceFingerprint,
                cancellationToken)
            .ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrEmpty(accessToken);

        var refreshToken = await jwt
            .GenerateRefreshTokenAsync(
                user.Id,
                familyId,
                new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id.ToString()) },
                client.IpAddress,
                client.UserAgent,
                client.DeviceFingerprint,
                cancellationToken)
            .ConfigureAwait(false);
        ArgumentException.ThrowIfNullOrEmpty(refreshToken);

        ctx.Set(BagKey.Qualify(stepKind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(stepKind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(stepKind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(stepKind, "ExpiresIn"), jwt.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(stepKind, "UserId"), user.Id);
    }
}
