namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="TokenStep"/> that invokes <c>TokenCommand(email, password)</c>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>name</c> — step name;</description></item>
/// <item><description><c>emailKey</c> — Bag key with e-mail/login (relative/absolute);</description></item>
/// <item><description><c>passwordKey</c> — Bag key with password (relative/absolute);</description></item>
/// <item><description><c>resultKey</c> — (opt.) key for storing the token; relative <c>"Token"</c> by default;</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// </summary>
internal sealed class TokenStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
        var userService = sp.GetRequiredService<IUserService>();

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

        return new TokenStep
        {
            Kind            = Kind,
            Logger          = loggerFactory.CreateLogger(nameof(TokenStep)),
            JwtTokenService = jwtTokenService,
            UserService     = userService,
            ResolveBy       = resolveBy,
            SelectorKey     = cfg.Str("selectorKey"),
            PasswordKey     = cfg.StrOpt("passwordKey"),
            CodeKey         = cfg.StrOpt("codeKey"),
            IpAddressKey    = cfg.Str("ipAddressKey"),
            UserAgentKey    = cfg.Str("userAgentKey"),
            DeviceFingerprintKey = cfg.Str("deviceFingerprintKey"),
            Next            = cfg.StrOpt("next")
        };
    }
}
