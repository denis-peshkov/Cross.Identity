namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ExternalLoginCompleteStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();
        var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
        var userService = sp.GetRequiredService<IUserService>();

        return new ExternalLoginCompleteStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            JwtTokenService      = jwtTokenService,
            UserService          = userService,
            CodeKey              = cfg.Str("codeKey"),
            StateKey             = cfg.Str("stateKey"),
            ErrorKey             = cfg.StrOpt("errorKey"),
            ErrorDescriptionKey  = cfg.StrOpt("errorDescriptionKey"),
            IpAddressKey         = cfg.Str("ipAddressKey"),
            UserAgentKey         = cfg.Str("userAgentKey"),
            DeviceFingerprintKey = cfg.Str("deviceFingerprintKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
