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
        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        // resolveBy is optional; if omitted, a sensible default is inferred from the channel
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("resetPassword: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        return new ResetPasswordStep
        {
            Kind               = Kind,
            SelectorKey        = cfg.Str("selectorKey"),
            PhoneNumberKey     = cfg.StrOpt("phoneNumberKey"),
            UserNameKey        = cfg.StrOpt("userNameKey"),
            PasswordKey        = cfg.Str("passwordKey"),
            IpAddressKey       = cfg.Str("ipAddressKey"),
            UserAgentKey       = cfg.Str("userAgentKey"),
            UserService        = userService,
            EmailSenderService = emailSenderService,
            SmsSenderService   = smsSenderService,
            Channel            = channel,
            ResolveBy          = new ResolveBy { Field = field },
            Logger             = loggerFactory.CreateLogger<ResetPasswordStep>(),
            Next               = cfg.StrOpt("next"),
        };
    }
}
