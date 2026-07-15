namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>Factory for <see cref="PasswordAuthStep"/>.</summary>
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
            SelectorKey   = cfg.Str("selectorKey"),               // may be relative/absolute
            PasswordKey   = cfg.Str("passwordKey"),               // may be relative/absolute
            UserIdKey     = cfg.StrOpt("userIdKey") ?? "UserId",  // relative by default → "{Name}.UserId"
            Next          = cfg.StrOpt("next")
        };
    }
}
