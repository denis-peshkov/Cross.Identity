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

    /// <summary>Key in <see cref="Bag"/> to read the client IP from. May be relative or absolute.</summary>
    public required string IpAddressKey { get; init; }

    /// <summary>Key in <see cref="Bag"/> to read the User-Agent from. May be relative or absolute.</summary>
    public required string UserAgentKey { get; init; }

    public required IExternalLoginService ExternalLoginService { get; init; }

    public required IJwtTokenService JwtTokenService { get; init; }

    public required IUserService UserService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Code may be absent when the provider returned Error (OAuth error redirect).
        ctx.TryGet(BagKey.Qualify(Kind, CodeKey), out string? code);
        var state = ctx.Get<string>(BagKey.Qualify(Kind, StateKey));
        string? error = null;
        string? errorDescription = null;
        if (!string.IsNullOrWhiteSpace(ErrorKey))
        {
            ctx.TryGet(BagKey.Qualify(Kind, ErrorKey), out error);
        }

        if (!string.IsNullOrWhiteSpace(ErrorDescriptionKey))
        {
            ctx.TryGet(BagKey.Qualify(Kind, ErrorDescriptionKey), out errorDescription);
        }

        var userId = await ExternalLoginService.CompleteAsync(code ?? string.Empty, state, error, errorDescription, cancellationToken).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId.UserId);
        ctx.Set(BagKey.Qualify(Kind, "IsLinking"), userId.IsLinking);

        if (userId.IsLinking)
        {
            return StepResult.Ok(Next);
        }

        var user = (await UserService.GetUserByAsync("Id", userId.UserId.ToString(), cancellationToken).ConfigureAwait(false)).ToBag();
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
        var username = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.UserId.ToString()),
            }
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);

        ctx.TryGet<string?>(BagKey.Qualify(Kind, IpAddressKey), out var ipAddress);
        ctx.TryGet<string?>(BagKey.Qualify(Kind, UserAgentKey), out var userAgent);

        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId.UserId, familyId, new List<string>(), accessClaims, ipAddress, userAgent, cancellationToken).ConfigureAwait(false);
        var refreshToken = await JwtTokenService
            .GenerateRefreshTokenAsync(
                userId.UserId,
                familyId,
                new List<Claim>
                {
                    new(JwtRegisteredClaimNames.Sub, userId.UserId.ToString())
                },
                ipAddress,
                userAgent,
                cancellationToken)
            .ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);

        return StepResult.Ok(Next);
    }
}
