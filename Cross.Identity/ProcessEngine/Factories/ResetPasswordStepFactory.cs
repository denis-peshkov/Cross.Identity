namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="ResetPasswordStep"/>.
/// </summary>
internal sealed class ResetPasswordStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory   = sp.GetRequiredService<ILoggerFactory>();
        var userService     = sp.GetRequiredService<IUserService>();
        var emailSenderService = sp.GetRequiredService<IEmailSenderService>();
        var smsSenderService = sp.GetRequiredService<ISmsSenderService>();
        var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        // resolveBy is optional; if omitted, a sensible default is inferred from the channel
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("resetPassword: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        return new ResetPasswordStep
        {
            Kind        = Kind,
            SelectorKey = cfg.Str("selectorKey"),
            PasswordKey = cfg.Str("passwordKey"),
            UserService = userService,
            EmailSenderService = emailSenderService,
            SmsSenderService = smsSenderService,
            HttpContextAccessor = httpContextAccessor,
            Channel = channel,
            ResolveBy   = new ResolveBy { Field = field },
            Logger      = loggerFactory.CreateLogger<ResetPasswordStep>(),
            Next        = cfg.StrOpt("next"),
        };
    }
}
