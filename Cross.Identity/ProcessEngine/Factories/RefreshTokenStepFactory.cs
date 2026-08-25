namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="RefreshTokenStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>jtiKey</c> — bag key for the refresh-token JTI (<c>RefreshTokens.Id</c>);</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// </summary>
internal sealed class RefreshTokenStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
        var userService = sp.GetRequiredService<IUserService>();
        var authenticationOptions = sp.GetRequiredService<IOptionsSnapshot<AuthenticationOptions>>().Value;

        return new RefreshTokenStep
        {
            Kind                  = Kind,
            JwtTokenService       = jwtTokenService,
            UserService           = userService,
            AuthenticationOptions = authenticationOptions,
            JtiKey                = cfg.Str("jtiKey"),
            Next                  = cfg.StrOpt("next")
        };
    }
}
