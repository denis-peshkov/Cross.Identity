namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>Фабрика шага <see cref="PasswordAuthStep"/>.</summary>
internal sealed class PasswordAuthStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var userService = sp.GetRequiredService<IUserService>();

        return new PasswordAuthStep
        {
            Kind          = Kind,
            UserService   = userService,
            SelectorField = cfg.Str("selectorField"),
            SelectorKey   = cfg.Str("selectorKey"),               // может быть относительным/абсолютным
            PasswordKey   = cfg.Str("passwordKey"),               // может быть относительным/абсолютным
            UserIdKey     = cfg.StrOpt("userIdKey") ?? "UserId",  // относительный по умолчанию → "{Name}.UserId"
            Next          = cfg.StrOpt("next")
        };
    }
}
