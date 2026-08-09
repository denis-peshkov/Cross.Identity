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

        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        return new SendCodeStep
        {
            Kind                      = Kind,
            Channel                   = channel,
            Selector                  = new Selector(),
            TtlKey                    = cfg.StrOpt("ttlKey"),
            CodeService               = codeService,
            UserService               = userService,
            Environment               = hostEnvironment,
            ProcessDefinitionProvider = processDefinitionProvider,
            Logger                    = loggerFactory.CreateLogger(nameof(SendCodeStep)),
            Configuration             = configuration,
            Next                      = cfg.StrOpt("next")
        };
    }
}
