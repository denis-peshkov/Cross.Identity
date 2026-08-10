namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="VerifyCodeStep"/>.
/// </summary>
internal sealed class VerifyCodeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        return new VerifyCodeStep
        {
            Kind        = Kind,
            Selector    = new Selector(),
            Channel     = cfg.EnumReq<ChannelEnum>("channel"),
            CodeKey     = cfg.Str("codeKey"),
            UserIdKey   = cfg.StrOpt("userIdKey") ?? "UserId",
            CodeService = sp.GetRequiredService<ICodeService>(),
            UserService = sp.GetRequiredService<IUserService>(),
            Next        = cfg.StrOpt("next")
        };
    }
}
