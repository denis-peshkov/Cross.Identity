using Cross.Identity.Services.ExternalOAuth;

namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class InitiateExternalLoginStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();

        return new InitiateExternalLoginStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            ProviderKey          = cfg.Str("providerKey"),
            ReturnUrlKey         = cfg.StrOpt("returnUrlKey"),
            LinkUserIdKey        = cfg.StrOpt("linkUserIdKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
