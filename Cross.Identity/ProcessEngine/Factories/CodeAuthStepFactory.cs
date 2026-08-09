namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CodeAuthStep"/>.
/// </summary>
internal sealed class CodeAuthStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService = sp.GetRequiredService<ICodeService>();
        var userService = sp.GetRequiredService<IUserService>();

        var channel = cfg.EnumOpt<ChannelEnum>("channel")
                      ?? throw new InvalidOperationException($"{Kind}: 'channel' is required.");

        return new CodeAuthStep
        {
            Kind        = Kind,
            CodeService = codeService,
            UserService = userService,
            Channel     = channel,
            Selector    = new Selector(),
            CodeKey     = cfg.Str("codeKey"),
            UserIdKey   = cfg.StrOpt("userIdKey") ?? "UserId",
            Next        = cfg.StrOpt("next")
        };
    }
}
