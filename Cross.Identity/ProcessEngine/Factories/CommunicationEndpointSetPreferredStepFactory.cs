namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class CommunicationEndpointSetPreferredStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        return new CommunicationEndpointSetPreferredStep
        {
            Kind                   = Kind,
            UserAccountIdKey       = cfg.Str("userAccountIdKey"),
            EndpointIdKey          = cfg.Str("endpointIdKey"),
            CommunicationEndpoints = sp.GetRequiredService<ICommunicationEndpointService>(),
            Next                   = cfg.StrOpt("next"),
        };
    }
}
