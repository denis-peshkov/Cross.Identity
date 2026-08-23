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
            Kind                   = Kind,
            Selector               = new Selector(),
            CodeKey                = cfg.Str("codeKey"),
            UserAccountIdKey              = cfg.StrOpt("userAccountIdKey") ?? "UserAccountId",
            CodeService            = sp.GetRequiredService<ICodeService>(),
            UserService            = sp.GetRequiredService<IUserService>(),
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            Logger                 = sp.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(VerifyCodeStep)),
            Next                   = cfg.StrOpt("next")
        };
    }
}
