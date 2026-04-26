namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="CodeAuthStep"/>.
/// <para>
/// Поддерживаемая JSON-схема шага (пример, где данные берём из предыдущего шага <c>auth-form</c>):
/// </para>
/// <code language="json">
/// {
///   "kind": "codeAuth",
///   "name": "code-auth",
///   "channel": "phone",
///   "identityKey": "auth-form.Phone",   // абсолютный ключ → читаем из шага "auth-form"
///   "codeKey":     "auth-form.Code",    // абсолютный ключ → читаем из шага "auth-form"
///   "resolveBy": { "field": "Phone" },
///   "userIdKey": "UserId",              // (опц.) относительный → сохранится как "code-auth.UserId"
///   "next": "issue"                     // (опц.) null — завершить процесс
/// }
/// </code>
/// <remarks>
/// Правила работы с ключами:
/// <list type="bullet">
/// <item><description>Относительный ключ (без точки) автоматически квалифицируется как <c>"{name}.{key}"</c>, где <c>name</c> — имя текущего шага.</description></item>
/// <item><description>Чтобы прочитать данные из другого шага, нужно указать абсолютный ключ: <c>"{other-step}.{Field}"</c>.</description></item>
/// </list>
/// </remarks>
/// </summary>
internal sealed class CodeAuthStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // Обязательные зависимости (из DI)
        var codeService = sp.GetRequiredService<ICodeService>();
        var userService = sp.GetRequiredService<IUserService>();

        // Обязательные поля шага
        var channel     = cfg.Str("channel");
        var identityKey = cfg.Str("identityKey");
        var codeKey     = cfg.Str("codeKey");

        // resolveBy.field
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("codeAuth: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        // Необязательные поля
        var userIdKey = cfg.StrOpt("userIdKey") ?? "UserId"; // относительный по умолчанию → "{Name}.UserId"
        var next      = cfg.StrOpt("next");

        return new CodeAuthStep
        {
            Kind        = Kind,
            CodeService = codeService,
            UserService = userService,
            Channel     = channel,
            IdentityKey = identityKey,
            CodeKey     = codeKey,
            ResolveBy   = new ResolveBy { Field = field },
            UserIdKey   = userIdKey,
            Next        = next
        };
    }
}
