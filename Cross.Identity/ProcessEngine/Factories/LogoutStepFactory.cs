namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="LogoutStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>jtiKey</c> — bag key for the access-token JTI;</description></item>
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
        return new LogoutStep
        {
            Kind            = Kind,
            JwtTokenService = sp.GetRequiredService<IJwtTokenService>(),
            JtiKey          = cfg.Str("jtiKey"),
            Next            = cfg.StrOpt("next"),
        };
    }
}
