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
internal sealed class TokenStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять e-mail/логин. Может быть относительным или абсолютным.</summary>
    public required string SelectorKey { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять пароль. Может быть относительным или абсолютным.</summary>
    public string? PasswordKey { get; init; }

    /// <summary>Ключ в <see cref="Bag"/>, откуда взять код. Может быть относительным или абсолютным.</summary>
    public string? CodeKey { get; init; }

    public ILogger Logger { get; set; }
    public IJwtTokenService JwtTokenService { get; set; }
    public IUserService UserService { get; set; }

    /// <summary>Настройки поиска пользователя: по какому полю искать (например, "Email" или "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) email/логин + пароль или код (абсолютные ключи вида collectForm.Email не префиксируются Kind шага token)
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
        string? passwordValue = null;
        if (PasswordKey != null)
        {
            ctx.TryGet(BagKey.Qualify(Kind, PasswordKey), out passwordValue);
        }

        string? codeValue = null;
        if (CodeKey != null)
        {
            ctx.TryGet(BagKey.Qualify(Kind, CodeKey), out codeValue);
        }

        // 2) валидация: при отсутствии PasswordKey в JSON нельзя трактовать "" как «пароль задан» — иначе ветка кода не выполняется (TokenByCode).
        var validated = false;
        if (PasswordKey != null && !string.IsNullOrEmpty(passwordValue))
        {
            validated = await UserService.ValidatePasswordAsync(ResolveBy.Field, selectorValue, passwordValue, cancellationToken);
        }
        else if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
        {
            validated = await UserService.ValidateCodeAsync(ResolveBy.Field, selectorValue, codeValue, cancellationToken);
        }
        if (!validated)
        {
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));
        }

        // 3) получаем данные юзера
        var user = (await UserService.GetUserByAsync(ResolveBy.Field, selectorValue, cancellationToken)).ToBag();
        ArgumentNullException.ThrowIfNull(user);
        var id     = user.TryGetValue("Id", out var idObj) && Guid.TryParse(idObj?.ToString(), out var guid) ? guid : Guid.Empty;
        var email = user.TryGetValue("Email", out var emailObj) ? emailObj?.ToString() : null;
        var phone = user.TryGetValue("Phone", out var phoneObj) ? phoneObj?.ToString() : null;
        var username    = user.TryGetValue("UserName", out var usernameObj) ? usernameObj?.ToString() : null;

        // 4) генерация AccessToken
        var familyId = Guid.NewGuid();
        var accessClaims = new List<Claim>
            {
                new (JwtRegisteredClaimNames.Sub, id.ToString()),
                // new (JwtRegisteredClaimNames.NameId, id),
                // new (ClaimTypes.NameIdentifier, id), // NameId ???
            }
            // .AddIfNotNull(JwtRegisteredClaimNames.Email, email)
            .AddIfNotNull(ClaimTypes.Email, email)
            .AddIfNotNull(ClaimTypes.MobilePhone, phone)
            .AddIfNotNull(ClaimConstants.Username, username);
        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(id, familyId, new List<string>(), accessClaims);

        // 5) генерация RefreshToken
        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(id, familyId, new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()) });

        // 6) сохраняем токен в Bag
        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), id);

        return StepResult.Ok(Next);
    }
}
