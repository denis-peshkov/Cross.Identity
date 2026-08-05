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

        // resolveBy is optional; if omitted, a sensible default is inferred from the channel
        ResolveBy resolveBy;
        if (cfg.TryGetProperty("resolveBy", out var resolveEl) && resolveEl.ValueKind == JsonValueKind.Object)
        {
            resolveBy = ResolveBy.FromJson(resolveEl);
            if (string.IsNullOrWhiteSpace(resolveBy.Field))
                throw new InvalidOperationException($"{Kind}: 'resolveBy.field' must be a non-empty string.");
        }
        else
        {
            resolveBy = ResolveBy.DefaultFor(channel);
        }

        return new SendCodeStep
        {
            Kind                      = Kind,
            Channel                   = channel,
            SelectorKey               = cfg.Str("selectorKey"),
            TtlKey                    = cfg.StrOpt("ttlKey"),
            CodeService               = codeService,
            UserService               = userService,
            Environment               = hostEnvironment,
            Configuration             = configuration,
            ProcessDefinitionProvider = processDefinitionProvider,
            Logger                    = loggerFactory.CreateLogger<SendCodeStep>(),
            ResolveBy                 = resolveBy,
            Next                      = cfg.StrOpt("next"),
        };
    }
}
