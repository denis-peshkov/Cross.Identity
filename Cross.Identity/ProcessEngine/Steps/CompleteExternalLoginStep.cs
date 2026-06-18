using Cross.Identity.Services.ExternalOAuth;

namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг завершения внешнего OAuth-логина: обмен кода, привязка аккаунта и выпуск JWT.
/// </summary>
internal sealed class CompleteExternalLoginStep : IStep
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
        var code = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));
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

        var userId = await ExternalLoginService.CompleteAsync(code, state, error, errorDescription, cancellationToken).ConfigureAwait(false);

        var user = (await UserService.GetUserByAsync("Id", userId.ToString(), cancellationToken).ConfigureAwait(false)).ToBag();
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
        var username = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            }
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);

        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, familyId, new List<string>(), accessClaims).ConfigureAwait(false);
        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(
            userId,
            familyId,
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) }).ConfigureAwait(false);

        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

        return StepResult.Ok(Next);
    }
}
