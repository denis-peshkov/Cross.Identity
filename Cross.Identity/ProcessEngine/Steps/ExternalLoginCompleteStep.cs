namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that completes external OAuth login: code exchange, account linking, and JWT issuance.
/// </summary>
internal sealed class ExternalLoginCompleteStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    public required string CodeKey { get; init; }

    public required string StateKey { get; init; }

    public string? ErrorKey { get; init; }

    public string? ErrorDescriptionKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    public required IJwtTokenService JwtTokenService { get; init; }

    public required IUserService UserService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Code may be absent when the provider returned Error (OAuth error redirect).
        var code = ctx.Get<string?>(BagKey.Qualify(Kind, CodeKey));
        var state = ctx.Get<string>(BagKey.Qualify(Kind, StateKey));
        var error = !string.IsNullOrWhiteSpace(ErrorKey)
            ? ctx.Get<string?>(BagKey.Qualify(Kind, ErrorKey))
            : null;
        var errorDescription = !string.IsNullOrWhiteSpace(ErrorDescriptionKey)
            ? ctx.Get<string?>(BagKey.Qualify(Kind, ErrorDescriptionKey))
            : null;

        var completion = await ExternalLoginService.CompleteAsync(code ?? string.Empty, state, error, errorDescription, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "UserId"), completion.UserId);
        ctx.Set(BagKey.Qualify(Kind, "IsLinking"), completion.IsLinking);

        if (completion.IsLinking)
        {
            return StepResult.Ok(Next);
        }

        var user = await UserService.GetUserByAsync("Id", completion.UserId.ToString(), cancellationToken).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(user);

        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            }
            .AddIfNotNull(ClaimTypes.Email, user.Email)
            .AddIfNotNull(ClaimTypes.MobilePhone, user.PhoneNumber)
            .AddIfNotNull(ClaimConstants.Username, user.UserName);

        var client = ClientContext.Read(ctx);

        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(user.Id, familyId, new List<string>(), accessClaims, client.IpAddress, client.UserAgent, client.DeviceFingerprint, cancellationToken).ConfigureAwait(false);
        var refreshToken = await JwtTokenService
            .GenerateRefreshTokenAsync(
                user.Id,
                familyId,
                new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                },
                client.IpAddress,
                client.UserAgent,
                client.DeviceFingerprint,
                cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);

        return StepResult.Ok(Next);
    }
}
