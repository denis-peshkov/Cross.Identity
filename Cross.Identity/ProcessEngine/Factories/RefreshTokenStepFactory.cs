namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="TokenStep"/> для вызова <c>TokenCommand(email, password)</c>.
/// JSON-параметры:
/// <list type="bullet">
/// <item><description><c>name</c> — имя шага;</description></item>
/// <item><description><c>refreshToken</c> Рефреш токен <c>"RefreshToken"</c>;</description></item>
/// <item><description><c>next</c> — (опц.) имя следующего шага; <c>null</c> — завершить.</description></item>
/// </list>
/// </summary>
internal sealed class RefreshTokenStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
        var userService = sp.GetRequiredService<IUserService>();
        var authenticationOptions = sp.GetRequiredService<IOptionsSnapshot<AuthenticationOptions>>().Value;
        var context = sp.GetRequiredService<IdentityContext>();

        return new RefreshTokenStep
        {
            Kind                  = Kind,
            Logger                = loggerFactory.CreateLogger(nameof(RefreshTokenStep)),
            JwtTokenService       = jwtTokenService,
            UserService           = userService,
            AuthenticationOptions = authenticationOptions,
            Context               = context,
            RefreshTokenKey       = cfg.Str("refreshTokenKey"),
            Next                  = cfg.StrOpt("next")
        };
    }
}
