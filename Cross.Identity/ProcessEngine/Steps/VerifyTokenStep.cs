namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Validates an access token against storage (not revoked, not expired).
/// Sets <c>Valid</c>; when valid, also <c>UserId</c> and <c>Jti</c> from claims when present.
/// Malformed tokens are treated as invalid (no throw).
/// </summary>
internal sealed class VerifyTokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public required string? Next { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the access token from. May be relative or absolute.</summary>
    public required string AccessTokenKey { get; init; }

    /// <summary>Service for working with JWT and token entities.</summary>
    public required IJwtTokenService JwtTokenService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        var accessToken = ctx.Get<string>(BagKey.Qualify(Kind, AccessTokenKey));

        var valid = false;
        try
        {
            valid = await JwtTokenService.ValidateAccessTokenAsync(accessToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            valid = false;
        }

        ctx.Set(BagKey.Qualify(Kind, "Valid"), valid);

        if (valid)
        {
            var sub = await JwtTokenService
                .GetClaimValueAsync(accessToken, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier)
                .ConfigureAwait(false);
            if (Guid.TryParse(sub, out var userId))
            {
                ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);
            }

            var jti = await JwtTokenService
                .GetClaimValueAsync(accessToken, JwtRegisteredClaimNames.Jti)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(jti))
            {
                ctx.Set(BagKey.Qualify(Kind, "Jti"), jti);
            }
        }

        return StepResult.Ok(Next);
    }
}
