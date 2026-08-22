namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ExternalLoginGetAllStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();

        return new ExternalLoginGetAllStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            UserIdKey            = cfg.Str("userIdKey"),
            RefreshTokenKey      = cfg.Str("refreshTokenKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
