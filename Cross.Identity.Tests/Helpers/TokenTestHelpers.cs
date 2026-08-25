namespace Cross.Identity.Tests.Helpers;

internal static class TokenTestHelpers
{
    public static string RefreshTokenHash(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    public static Guid RefreshTokenJti(IJwtTokenService jwt, string refreshToken) =>
        Guid.Parse(jwt.GetClaimValue(refreshToken, JwtRegisteredClaimNames.Jti)!);

    public static async Task<bool> IsRefreshTokenActiveAsync(
        IdentityContext context,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var hash = RefreshTokenHash(refreshToken);
        var entity = await context.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken)
            .ConfigureAwait(false);
        return entity is { RevokedAt: null }
               && entity.ExpiresAt >= DateTime.UtcNow
               && entity.AbsoluteExpiresAt >= DateTime.UtcNow;
    }

    public static async Task RevokeAccessTokenByJtiAsync(IdentityContext context, Guid jti, CancellationToken cancellationToken)
    {
        var entity = await context.AccessTokens.FindAsync(new object[] { jti }, cancellationToken).ConfigureAwait(false);
        if (entity is not null)
        {
            entity.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
