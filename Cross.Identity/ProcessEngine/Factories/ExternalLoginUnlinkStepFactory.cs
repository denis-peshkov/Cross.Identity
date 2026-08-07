namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ExternalLoginUnlinkStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();

        return new ExternalLoginUnlinkStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            ProviderKey          = cfg.Str("providerKey"),
            UserIdKey            = cfg.Str("userIdKey"),
            IpAddressKey         = cfg.Str("ipAddressKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
