namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="ResetPasswordStep"/>.
/// </summary>
internal sealed class ResetPasswordStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService     = sp.GetRequiredService<ICodeService>();
        var loggerFactory   = sp.GetRequiredService<ILoggerFactory>();
        var userService     = sp.GetRequiredService<IUserService>();
        var hostEnvironment = sp.GetRequiredService<IHostEnvironment>();

        // ttlSeconds (опционально), по умолчанию 5 минут
        var ttl = cfg.TimeSpanSecondsOpt("ttlSeconds") ?? TimeSpan.FromMinutes(5);

        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        // resolveBy — объект опционален; если не задан, берём разумный дефолт от канала
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("codeAuth: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        return new ResetPasswordStep
        {
            Kind        = Kind,
            Channel     = channel,
            SelectorKey = cfg.Str("selectorKey"),
            UserService = userService,
            ResolveBy   = new ResolveBy { Field = field },
            Logger      = loggerFactory.CreateLogger<SendCodeStep>(),
            Next        = cfg.StrOpt("next"),
            PasswordKey = null,
        };
    }
}
