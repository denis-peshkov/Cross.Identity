namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Logout current session: revokes the presented refresh token and access tokens in the same
/// family with <see cref="RefreshTokenRevokedReason.USER_LOGOUT"/>.
/// Missing or already-revoked tokens are a no-op (idempotent).
/// </summary>
internal sealed class LogoutStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the source refresh token from. May be relative or absolute.</summary>
    public required string RefreshTokenKey { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var refreshToken = ctx.Get<string>(BagKey.Qualify(Kind, RefreshTokenKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);

        await JwtTokenService.RevokeRefreshTokenForLogoutAsync(refreshToken, hostSuppliedClientContext, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Revoked"), true);

        return StepResult.Ok(Next);
    }
}
