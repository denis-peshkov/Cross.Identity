namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="VerifyTokenStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>accessTokenKey</c> — bag key for the access token;</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// </summary>
internal sealed class VerifyTokenStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();

        return new VerifyTokenStep
        {
            Kind             = Kind,
            JwtTokenService  = jwtTokenService,
            AccessTokenKey   = cfg.Str("accessTokenKey"),
            Next             = cfg.StrOpt("next"),
        };
    }
}
