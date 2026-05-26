namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг проверки аутентификации по паролю.
/// Использует <see cref="IUserService"/> для поиска пользователя и валидации пароля.
/// <para>
/// Правила ключей:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> и <see cref="PasswordKey"/> — если относительные (без точки),
///     читаются как <c>"{Name}.{Key}"</c>; чтобы читать данные из другого шага, укажи абсолютные ключи
///     вида <c>"other-step.Field"</c>.</description></item>
///   <item><description><see cref="UserIdKey"/> — если относительный, результат записывается как <c>"{Name}.{UserIdKey}"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class PasswordAuthStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Сервис пользователей для проверки пароля.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Поле для поиска пользователя (например, "Email" или "UserName").</summary>
    public required string SelectorField { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда взять значение селектора (например, "auth-form.Email").
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.SelectorKey"</c>) или абсолютным.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда взять пароль (например, "auth-form.Password").
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.PasswordKey"</c>) или абсолютным.
    /// </summary>
    public required string PasswordKey { get; init; }

    /// <summary>
    /// Ключ для сохранения идентификатора пользователя в <see cref="Bag"/>.
    /// Если ключ относительный (без точки), он будет сохранён как <c>"{Kind}.UserIdKey"</c>.
    /// По умолчанию <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; } = "UserId";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // читать: относительные ключи квалифицируем префиксом имени шага
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));
        var password      = ctx.Get<string>(BagKey.Qualify(Kind, PasswordKey));

        // валидация пароля
        var ok = await UserService.ValidatePasswordAsync(SelectorField, selectorValue, password, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid credentials."));

        // резолв идентификатора
        var userId = await UserService.GetUserIdByAsync(SelectorField, selectorValue, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("User not found after password validation.");

        // писать: относительный ключ → "{Kind}.UserIdKey"
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
