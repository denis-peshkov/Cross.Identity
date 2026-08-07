namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ExternalLoginInitiateStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();

        return new ExternalLoginInitiateStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            ProviderKey          = cfg.Str("providerKey"),
            ReturnUrlKey         = cfg.StrOpt("returnUrlKey"),
            UserIdKey        = cfg.StrOpt("userIdKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
