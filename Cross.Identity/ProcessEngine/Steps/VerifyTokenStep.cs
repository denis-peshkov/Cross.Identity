namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Validates an access token (crypto + storage) via <see cref="IJwtTokenService.ValidateAccessTokenAsync"/>.
/// Sets <c>Valid</c>; when valid, also <c>UserId</c> and <c>Jti</c> from claims.
/// Invalid or malformed tokens yield <c>Valid = false</c> with <see cref="StepResult.Ok"/>.
/// Operational failures (database, configuration, etc.) return <see cref="StepResult.Fail"/>.
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

        bool valid;
        try
        {
            valid = await JwtTokenService.ValidateAccessTokenAsync(accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            valid = false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StepResult.Fail(ex);
        }

        ctx.Set(BagKey.Qualify(Kind, "Valid"), valid);

        if (valid)
        {
            var sub = JwtTokenService.GetClaimValue(accessToken, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
            if (Guid.TryParse(sub, out var userAccountId))
            {
                ctx.Set(BagKey.Qualify(Kind, "UserId"), userAccountId);
            }

            var jti = JwtTokenService.GetClaimValue(accessToken, JwtRegisteredClaimNames.Jti);
            if (!string.IsNullOrEmpty(jti))
            {
                ctx.Set(BagKey.Qualify(Kind, "Jti"), jti);
            }
        }

        return StepResult.Ok(Next);
    }
}
