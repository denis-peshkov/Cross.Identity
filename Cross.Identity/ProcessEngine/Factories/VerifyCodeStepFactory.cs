namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="VerifyCodeStep"/>.
/// </summary>
internal sealed class VerifyCodeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService = sp.GetRequiredService<ICodeService>();

        var channel = cfg.EnumOpt<ChannelEnum>("channel");

        return new VerifyCodeStep
        {
            Kind        = Kind,
            Selector    = new Selector(),
            Channel     = channel,
            CodeKey     = cfg.Str("codeKey"),
            CodeService = codeService,
            Next        = cfg.StrOpt("next")
        };
    }
}
