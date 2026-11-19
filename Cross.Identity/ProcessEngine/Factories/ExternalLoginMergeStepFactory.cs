namespace Cross.Identity.ProcessEngine.Factories;

internal sealed class ExternalLoginMergeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var externalLoginService = sp.GetRequiredService<IExternalLoginService>();

        return new ExternalLoginMergeStep
        {
            Kind                 = Kind,
            ExternalLoginService = externalLoginService,
            MergeTokenKey        = cfg.Str("mergeTokenKey"),
            UserAccountIdKey     = cfg.Str("userAccountIdKey"),
            Next                 = cfg.StrOpt("next"),
        };
    }
}
