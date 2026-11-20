namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CommunicationEndpointSetVerifiedStep"/>.
/// </summary>
internal sealed class CommunicationEndpointSetVerifiedStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new CommunicationEndpointSetVerifiedStep
        {
            Kind                   = Kind,
            UserAccountIdKey       = cfg.Str("userAccountIdKey"),
            EmailKey               = cfg.Str("emailKey"),
            PhoneKey               = cfg.Str("phoneKey"),
            CodeKey                = cfg.Str("codeKey"),
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            CodeService            = sp.GetRequiredService<ICodeService>(),
            Logger                 = loggerFactory.CreateLogger(nameof(CommunicationEndpointSetVerifiedStep)),
            Next                   = cfg.StrOpt("next"),
        };
    }
}
