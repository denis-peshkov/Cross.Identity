namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="ForgotPasswordStep"/>.
/// </summary>
internal sealed class ForgotPasswordStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService               = sp.GetRequiredService<ICodeService>();
        var loggerFactory             = sp.GetRequiredService<ILoggerFactory>();
        var configuration             = sp.GetRequiredService<IConfiguration>();
        var hostEnvironment           = sp.GetRequiredService<IHostEnvironment>();
        var processDefinitionProvider = sp.GetRequiredService<IProcessDefinitionProvider>();

        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        return new ForgotPasswordStep
        {
            Kind                      = Kind,
            Next                      = cfg.StrOpt("next"),
            CodeService               = codeService,
            Configuration             = configuration,
            Environment               = hostEnvironment,
            Logger                    = loggerFactory.CreateLogger<ForgotPasswordStep>(),
            ProcessDefinitionProvider = processDefinitionProvider,
            UserService               = sp.GetRequiredService<IUserService>(),
            CommunicationEndpoints    = sp.GetRequiredService<ICommunicationEndpointService>(),
            Channel                   = channel,
            Selector                  = new Selector(),
        };
    }
}
