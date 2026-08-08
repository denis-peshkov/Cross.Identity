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

        // resolveBy is optional; if omitted, a sensible default is inferred from the channel
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("codeAuth: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        return new ForgotPasswordStep
        {
            Kind                      = Kind,
            Next                      = cfg.StrOpt("next"),
            CodeService               = codeService,
            Configuration             = configuration,
            Environment               = hostEnvironment,
            Logger                    = loggerFactory.CreateLogger<ForgotPasswordStep>(),
            ProcessDefinitionProvider = processDefinitionProvider,
            Channel                   = channel,
            ResolveBy                 = new ResolveBy { Field = field },
            SelectorKey               = cfg.Str("selectorKey"),
            PhoneNumberKey            = cfg.StrOpt("phoneNumberKey"),
            UserNameKey               = cfg.StrOpt("userNameKey"),
            PasswordKey               = string.Empty,
        };
    }
}
