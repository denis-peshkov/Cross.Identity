namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CommunicationEndpointAddStep"/>.
/// </summary>
internal sealed class CommunicationEndpointAddStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new CommunicationEndpointAddStep
        {
            Kind                   = Kind,
            UserAccountIdKey       = cfg.Str("userAccountIdKey"),
            EmailKey               = cfg.Str("emailKey"),
            PhoneKey               = cfg.Str("phoneKey"),
            TtlKey                 = cfg.StrOpt("ttlKey"),
            Template               = cfg.Str("template"),
            Subject                = cfg.Str("subject"),
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            CodeService            = sp.GetRequiredService<ICodeService>(),
            NotificationComposer   = sp.GetRequiredService<INotificationComposer>(),
            Configuration          = sp.GetRequiredService<IConfiguration>(),
            Logger                 = loggerFactory.CreateLogger(nameof(CommunicationEndpointAddStep)),
            Next                   = cfg.StrOpt("next"),
        };
    }
}
