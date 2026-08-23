namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class CommunicationEndpointsGetAllStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        return new CommunicationEndpointsGetAllStep
        {
            Kind                   = Kind,
            UserAccountIdKey              = cfg.Str("userAccountIdKey"),
            RefreshTokenKey        = cfg.Str("refreshTokenKey"),
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            Next                   = cfg.StrOpt("next"),
        };
    }
}
