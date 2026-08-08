namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="VerifyCodeStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>name</c> — step name;</description></item>
/// <item><description><c>channel</c> — verification channel (<c>email</c> / <c>sms</c>, …);</description></item>
/// <item><description><c>identityKey</c> — Bag key for the identifier (email/phone/username);
///     if relative (no dot) → read as <c>"{name}.identityKey"</c>;
///     if absolute (with a dot), used as-is.</description></item>
/// <item><description><c>codeKey</c> — Bag key for the verification code;
///     follows the same rules (relative/absolute).</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish the process.</description></item>
/// </list>
/// </summary>
internal sealed class VerifyCodeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService = sp.GetRequiredService<ICodeService>();

        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        return new VerifyCodeStep
        {
            Kind           = Kind,
            Channel        = channel,
            IdentityKey    = cfg.Str("identityKey"),
            PhoneNumberKey = cfg.StrOpt("phoneNumberKey"),
            UserNameKey    = cfg.StrOpt("userNameKey"),
            CodeKey        = cfg.Str("codeKey"),
            CodeService    = codeService,
            Next           = cfg.StrOpt("next")
        };
    }
}
