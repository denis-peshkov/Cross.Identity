namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Logout current session: resolves the token family from access-token <c>jti</c> and revokes the whole family
/// with <see cref="RefreshTokenRevokedReason.USER_LOGOUT"/>.
/// Missing or already-revoked JTI is a no-op (idempotent).
/// </summary>
internal sealed class LogoutStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the access-token JTI from. May be relative or absolute.</summary>
    public required string JtiKey { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var jti = ctx.Get<Guid>(BagKey.Qualify(Kind, JtiKey));
        var hostSuppliedClientContext = HostSuppliedClientContext.Read(ctx);

        await JwtTokenService.RevokeSessionForLogoutAsync(jti, hostSuppliedClientContext, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "Revoked"), true);

        return StepResult.Ok(Next);
    }
}
