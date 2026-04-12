namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="GetUserIdStep"/>.
/// JSON-параметры:
/// <list type="bullet">
/// <item><description><c>name</c> — имя шага;</description></item>
/// <item><description><c>selectorField</c> — поле поиска ("Email" | "UserName" | "Phone" | ...);</description></item>
/// <item><description><c>selectorKey</c> — ключ в Bag, откуда брать значение селектора:
///   относительный (без точки) → будет прочитан как <c>"{name}.selectorKey"</c>,
///   абсолютный (с точкой) используется как есть;</description></item>
/// <item><description><c>userIdKey</c> — (опц.) ключ для записи результата; по умолчанию относительный <c>"UserId"</c>
///   → будет сохранён как <c>"{name}.UserId"</c>;</description></item>
/// <item><description><c>next</c> — (опц.) имя следующего шага, <c>null</c> — завершить.</description></item>
/// </list>
/// </summary>
internal sealed class GetUserIdStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var userService = sp.GetRequiredService<IUserService>();

        return new GetUserIdStep
        {
            Kind          = Kind,
            UserService   = userService,
            SelectorField = cfg.Str("selectorField"),
            SelectorKey   = cfg.Str("selectorKey"),
            Next          = cfg.StrOpt("next")
        };
    }
}
