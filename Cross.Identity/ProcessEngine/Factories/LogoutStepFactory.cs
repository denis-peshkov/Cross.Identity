namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="LogoutStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>refreshTokenKey</c> — bag key for the refresh token;</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// </summary>
internal sealed class LogoutStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();

        return new LogoutStep
        {
            Kind            = Kind,
            JwtTokenService = jwtTokenService,
            RefreshTokenKey = cfg.Str("refreshTokenKey"),
            IpAddressKey    = cfg.Str("ipAddressKey"),
            UserAgentKey    = cfg.Str("userAgentKey"),
            Next            = cfg.StrOpt("next"),
        };
    }
}
