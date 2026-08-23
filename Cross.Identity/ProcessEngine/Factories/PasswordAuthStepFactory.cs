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
            Kind        = Kind,
            UserService = userService,
            Selector    = new Selector(),
            PasswordKey = cfg.Str("passwordKey"),
            UserAccountIdKey   = cfg.StrOpt("userAccountIdKey") ?? "UserAccountId",
            Next        = cfg.StrOpt("next")
        };
    }
}
