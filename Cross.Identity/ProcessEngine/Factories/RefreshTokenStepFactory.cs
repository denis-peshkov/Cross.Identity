namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="TokenStep"/> that invokes <c>TokenCommand(email, password)</c>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>name</c> — step name;</description></item>
/// <item><description><c>refreshToken</c> — refresh token key <c>"RefreshToken"</c>;</description></item>
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
            RefreshTokenKey       = cfg.Str("refreshTokenKey"),
            Next                  = cfg.StrOpt("next")
        };
    }
}
