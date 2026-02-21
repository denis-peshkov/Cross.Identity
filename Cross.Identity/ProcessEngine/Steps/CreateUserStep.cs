namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг создания нового пользователя в системе.
/// Использует <see cref="IUserService"/> для записи пользователя
/// и сохраняет его идентификатор в <see cref="Bag"/>.
/// <para>
/// Сценарий:
/// <list type="number">
///   <item><c>collectForm</c> собирает регистрационные данные (email, userName, phone и пр.) и пишет их в Bag с префиксом имени шага (например, <c>reg-form.Email</c>).</item>
///   <item><c>CreateUserStep</c> отображает эти данные в карту <see cref="Map"/>: значения берутся по ключам из Bag; ключ может быть
///       абсолютным (например, <c>"reg-form.Email"</c>) или относительным (тогда будет использовано <c>"{Name}.Email"</c>).</item>
///   <item>Вызов <see cref="IUserService.CreateUserAsync"/> создаёт пользователя и возвращает Id.</item>
///   <item>Id сохраняется в <see cref="Bag"/> под ключом <see cref="UserIdKey"/> (относительный ключ будет сохранён как <c>"{Name}.UserIdKey"</c>).</item>
/// </list>
/// </para>
/// </summary>
internal sealed class CreateUserStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Сервис пользователей.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>
    /// Словарь "поле пользователя → ключ в <see cref="Bag"/>".
    /// Значения могут быть абсолютными (например, <c>"reg-form.Email"</c>) или относительными
    /// (в этом случае будет использовано <c>"{Kind}.Email"</c>).
    /// <para>
    /// Пример:
    /// <code language="json">
    /// "map": {
    ///   "Email": "reg-form.Email",
    ///   "UserName": "reg-form.UserName",
    ///   "Phone": "Phone" // относительный → "{Kind}.Phone"
    /// }
    /// </code>
    /// </para>
    /// </summary>
    public required IDictionary<string, string> Map { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, куда будет записан Id созданного пользователя.
    /// Если ключ относительный (без точки), он будет сохранён как <c>"{Kind}.UserIdKey"</c>.
    /// По умолчанию <c>"UserId"</c>.
    /// </summary>
    public string UserIdKey { get; init; }

    public string SelectorKey { get; set; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // Собираем карту значений пользователя из Bag
        var userFields = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (userField, bagKeyRaw) in Map)
        {
            // относительный ключ → "{Kind}.{bagKeyRaw}"
            var bagKey = BagKey.Qualify(Kind, bagKeyRaw);

            if (ctx.TryGet<object?>(bagKey, out var value))
                userFields[userField] = value;
        }

        // Создаём пользователя
        var userId = await UserService.CreateUserAsync(userFields, cancellationToken);

        // Сохраняем Id в Bag (относительный → "{Kind}.UserIdKey")
        ctx.Set(BagKey.Qualify(Kind, UserIdKey), userId);
        ctx.TryGet<string>(SelectorKey, out var selectorKey);
        ctx.Set(BagKey.Qualify(Kind, "selectorKey"), selectorKey);

        return StepResult.Ok(Next);
    }
}
