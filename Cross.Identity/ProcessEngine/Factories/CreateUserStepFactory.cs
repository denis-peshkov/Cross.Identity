namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="CreateUserStep"/>.
/// </summary>
internal sealed class CreateUserStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var userService = sp.GetRequiredService<IUserService>();

        // карта "поле пользователя" -> "ключ в Bag" (может быть абсолютным или относительным)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in cfg.GetProperty("map").EnumerateObject())
            map[p.Name] = p.Value.GetString()!;

        return new CreateUserStep
        {
            Kind        = Kind,
            UserService = userService,
            Map         = map,
            SelectorKey = cfg.Str("selectorKey"),
            UserIdKey   = cfg.StrOpt("userIdKey") ?? "UserId", // относительный по умолчанию
            Next        = cfg.StrOpt("next")
        };
    }
}
