namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг аутентификации пользователя (по паролю или коду)
/// и выпуска пары JWT-токенов (access + refresh).
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/>, <see cref="PasswordKey"/> и <see cref="CodeKey"/>:
///     если ключ относительный (без точки), читается как <c>"{Kind}.{Key}"</c>;
///     чтобы читать данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
///   <item><description>Результат всегда пишется в ключи:
///     <c>AccessToken</c>, <c>RefreshToken</c>, <c>TokenType</c>, <c>ExpiresIn</c>, <c>UserId</c>
///     (с префиксом <c>{Kind}.</c> для относительного доступа).</description></item>
/// </list>
/// </para>
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

    /// <summary>Логгер шага.</summary>
    public ILogger Logger { get; set; }

    /// <summary>Сервис выпуска токенов.</summary>
    public IJwtTokenService JwtTokenService { get; set; }

    /// <summary>Сервис проверки учетных данных и чтения пользователя.</summary>
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
            validated = await UserService.ValidatePasswordAsync(ResolveBy.Field, selectorValue, passwordValue, cancellationToken).ConfigureAwait(false);
        }
        else if (CodeKey != null && !string.IsNullOrEmpty(codeValue))
        {
            validated = await UserService.ValidateCodeAsync(ResolveBy.Field, selectorValue, codeValue, cancellationToken).ConfigureAwait(false);
        }
        if (!validated)
        {
            ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), true);
            return StepResult.Ok(Next);//new NotAuthorizedException("Invalid credentials."));
        }

        // 3) получаем данные юзера
        var user = (await UserService.GetUserByAsync(ResolveBy.Field, selectorValue, cancellationToken).ConfigureAwait(false)).ToBag();
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
        var accessToken = await JwtTokenService.GenerateAccessTokenAsync(id, familyId, new List<string>(), accessClaims).ConfigureAwait(false);

        // 5) генерация RefreshToken
        var refreshToken = await JwtTokenService.GenerateRefreshTokenAsync(id, familyId, new List<Claim> { new(JwtRegisteredClaimNames.Sub, id.ToString()) }).ConfigureAwait(false);

        // 6) сохраняем токен в Bag
        ctx.Set(BagKey.Qualify(Kind, "AccessToken"), accessToken);
        ctx.Set(BagKey.Qualify(Kind, "RefreshToken"), refreshToken);
        ctx.Set(BagKey.Qualify(Kind, "TokenType"), "Bearer");
        ctx.Set(BagKey.Qualify(Kind, "ExpiresIn"), JwtTokenService.AccessTokenExpiresInSeconds);
        ctx.Set(BagKey.Qualify(Kind, "UserId"), id);
        ctx.Set(BagKey.Qualify(Kind, "IsInvalidCode"), false);

        return StepResult.Ok(Next);
    }
}
