namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="LogoutAllStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>userAccountIdKey</c> — bag key for the user account id;</description></item>
/// <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// </summary>
internal sealed class LogoutAllStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        return new LogoutAllStep
        {
            Kind              = Kind,
            JwtTokenService   = sp.GetRequiredService<IJwtTokenService>(),
            UserAccountIdKey  = cfg.Str("userAccountIdKey"),
            Next              = cfg.StrOpt("next"),
        };
    }
}
