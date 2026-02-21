namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг выпуска JWT-токена через MediatR-команду приложения
/// <c>TokenCommand(string email, string password)</c>.
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> и <see cref="PasswordKey"/>:
///     если ключ относительный (без точки), читается как <c>"{Name}.{Key}"</c>;
///     чтобы читать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="ResultKey"/> — если ключ относительный, записывается как <c>"{Name}.{ResultKey}"</c>.</description></item>
/// </list>
/// </para>
/// Ожидается, что результат обработчика содержит строковое свойство <c>AccessToken</c>
/// (или <c>Token</c>), либо сам является строкой. Значение будет записано в <see cref="Bag"/>.
/// </summary>
internal sealed class ResetPasswordStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять e-mail/логин. Может быть относительным или абсолютным.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять пароль. Может быть относительным или абсолютным.</summary>
    public required string PasswordKey { get; init; }

    public string AccessTokenKey { get; init; }
    public string RefreshTokenKey { get; init; }
    public string TokenTypeKey { get; init; }
    public string ExpiresInKey { get; init; }


    public ILogger Logger { get; set; }
    public IJwtTokenService JwtTokenService { get; set; }
    public IUserService UserService { get; set; }

    /// <summary>Канал доставки кода (например, <c>"email"</c> или <c>"phone"</c>).</summary>
    public required ChannelEnum Channel { get; init; }

    public ResolveBy ResolveBy { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) достаём email и пароль (с учётом относительных ключей)
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
        var passwordValue = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));

        // 2) получаем данные юзера
        var user = (await UserService.GetUserByAsync("", selectorValue, cancellationToken)).ToBag();
        var userId = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
        var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        // 3) open Transaction
        // var transactionOptions = new TransactionOptions
        // {
        //     IsolationLevel = IsolationLevel.ReadCommitted,
        //     Timeout = TimeSpan.FromSeconds(60)
        // };
        // using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

        // 5) генерация AccessToken
        var accessClaims = new List<Claim>()
            .AddIfNotNull(JwtRegisteredClaimNames.Sub, userId.ToString())
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);
        // var accessToken = await JwtTokenService.GenerateAccessTokenAsync(userId, oldRefreshToken.FamilyId, new List<string>(), accessClaims);
        //
        // var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(userId, email, phone, username);

        // 8) Complete Transaction
        // scope.Complete();

        // 4) сохраняем токен в Bag
        // ctx.Set(BagKey.Qualify(Kind, AccessTokenKey), accessToken);
        // ctx.Set(BagKey.Qualify(Kind, RefreshTokenKey), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, TokenTypeKey), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);

        return StepResult.Ok(Next);
    }
}
