namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="TokenStep"/> для вызова <c>TokenCommand(email, password)</c>.
/// JSON-параметры:
/// <list type="bullet">
/// <item><description><c>name</c> — имя шага;</description></item>
/// <item><description><c>emailKey</c> — ключ в Bag с e-mail/логином (относительный/абсолютный);</description></item>
/// <item><description><c>passwordKey</c> — ключ в Bag с паролем (относительный/абсолютный);</description></item>
/// <item><description><c>resultKey</c> — (опц.) ключ для записи токена; по умолчанию относительный <c>"Token"</c>;</description></item>
/// <item><description><c>next</c> — (опц.) имя следующего шага; <c>null</c> — завершить.</description></item>
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

        // resolveBy — объект опционален; если не задан, берём разумный дефолт от канала
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
            Next            = cfg.StrOpt("next")
        };
    }
}
