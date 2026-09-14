namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ChangeAccountEmailStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        return new ChangeAccountEmailStep
        {
            Kind             = Kind,
            UserAccountIdKey = cfg.Str("userAccountIdKey"),
            EmailKey         = cfg.Str("emailKey"),
            UserService      = sp.GetRequiredService<IUserService>(),
            Next             = cfg.StrOpt("next"),
        };
    }
}
