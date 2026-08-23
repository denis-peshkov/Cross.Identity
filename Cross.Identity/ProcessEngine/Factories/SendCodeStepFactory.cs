namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="SendCodeStep"/>.
/// </summary>
internal sealed class SendCodeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService               = sp.GetRequiredService<ICodeService>();
        var loggerFactory             = sp.GetRequiredService<ILoggerFactory>();
        var userService               = sp.GetRequiredService<IUserService>();
        var hostEnvironment           = sp.GetRequiredService<IHostEnvironment>();
        var processDefinitionProvider = sp.GetRequiredService<IProcessDefinitionProvider>();
        var configuration             = sp.GetRequiredService<IConfiguration>();

        return new SendCodeStep
        {
            Kind                      = Kind,
            Selector                  = new Selector(),
            TtlKey                    = cfg.StrOpt("ttlKey"),
            Template                  = cfg.Str("template"),
            Subject                   = cfg.Str("subject"),
            CodeService               = codeService,
            UserService               = userService,
            CommunicationEndpoints    = sp.GetRequiredService<ICommunicationEndpointService>(),
            Environment               = hostEnvironment,
            ProcessDefinitionProvider = processDefinitionProvider,
            Logger                    = loggerFactory.CreateLogger(nameof(SendCodeStep)),
            Configuration             = configuration,
            Next                      = cfg.StrOpt("next")
        };
    }
}
