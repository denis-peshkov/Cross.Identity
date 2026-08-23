namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="TokenStep"/>.
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

        return new TokenStep
        {
            Kind                 = Kind,
            Logger               = loggerFactory.CreateLogger(nameof(TokenStep)),
            JwtTokenService      = jwtTokenService,
            UserService          = userService,
            Selector             = new Selector(),
            PasswordKey          = cfg.StrOpt("passwordKey"),
            CodeKey              = cfg.StrOpt("codeKey"),
            Next                 = cfg.StrOpt("next")
        };
    }
}
