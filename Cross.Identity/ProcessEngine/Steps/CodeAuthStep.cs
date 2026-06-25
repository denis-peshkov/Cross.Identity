namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг аутентификации пользователя по одноразовому коду (OTP).
/// Использует <see cref="ICodeService"/> для проверки кода и <see cref="IUserService"/> для поиска пользователя.
/// <para>
/// Сценарий:
/// <list type="number">
///   <item><c>collectForm</c> собирает Email/Phone и одноразовый код и пишет их в Bag c префиксом имени своего шага (например, <c>auth-form.Email</c>).</item>
///   <item><c>CodeAuthStep</c> читает идентичность и код по ключам <see cref="IdentityKey"/> и <see cref="CodeKey"/>:
///       если ключ относительный (без точки), он автоматически квалифицируется как <c>"{Name}.{key}"</c>;
///       если абсолютный (с точкой) — используется как есть.</item>
///   <item>Если код валиден, ищет пользователя по полю <see cref="ResolveBy.Field"/> и сохраняет его Id в Bag по ключу <see cref="UserIdKey"/>
///       (относительный ключ → <c>"{Name}.{UserIdKey}"</c>).</item>
///   <item>Если код неверный или пользователь не найден — возвращает ошибку.</item>
/// </list>
/// </para>
/// </summary>
internal sealed class CodeAuthStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Сервис одноразовых кодов.</summary>
    public required ICodeService CodeService { get; init; }

    /// <summary>Сервис пользователей.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Канал проверки (например, "email" или "phone").</summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда брать идентичность (например, <c>"auth-form.Email"</c>).
    /// Может быть относительным (тогда будет квалифицирован в <c>"{Kind}.IdentityKey"</c>).
    /// </summary>
    public required string IdentityKey { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда брать предъявленный код.
    /// Может быть относительным (тогда будет квалифицирован в <c>"{Kind}.CodeKey"</c>).
    /// </summary>
    public required string CodeKey { get; init; }

    /// <summary>Настройки поиска пользователя: по какому полю искать (например, "Email" или "Phone").</summary>
    public required ResolveBy ResolveBy { get; init; }

    /// <summary>
    /// Ключ для сохранения идентификатора пользователя.
    /// Если ключ относительный (без точки), он будет сохранён как <c>"{Kind}.UserIdKey"</c>.
    /// По умолчанию <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; } = "Id";

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Читать: относительные ключи квалифицируем префиксом имени шага
        var identity = ctx.Get<string>(BagKey.Qualify(Kind, IdentityKey));
        var code     = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        // 1) Проверка кода
        var ok = await CodeService.VerifyAsync(Channel, identity, code, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired code."));

        // 2) Резолв пользователя
        var userId = await UserService.GetUserIdByAsync(ResolveBy.Field, identity, cancellationToken).ConfigureAwait(false);
        if (userId is null)
        {
            return StepResult.Fail(new KeyNotFoundException("User not found."));
        }

        // Писать: относительный ключ → "{Kind}.UserIdKey"
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);

        return StepResult.Ok(Next);
    }
}
