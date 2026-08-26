namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Logout from all devices: revokes every active access/refresh token for the user identified by
/// <see cref="UserAccountIdKey"/> with <see cref="RefreshTokenRevokedReason.USER_LOGOUT_ALL"/>.
/// The host must authorize the caller for that account; this step does not require a refresh token.
/// </summary>
internal sealed class LogoutAllStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the user account id from. May be relative or absolute.</summary>
    public required string UserAccountIdKey { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var userAccountId = ctx.Get<Guid>(BagKey.Qualify(Kind, UserAccountIdKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);

        await JwtTokenService.RevokeAllTokensForUserAsync(
                userAccountId,
                RefreshTokenRevokedReason.USER_LOGOUT_ALL,
                hostSuppliedClientContext,
                cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Revoked"), true);

        return StepResult.Ok(Next);
    }
}
