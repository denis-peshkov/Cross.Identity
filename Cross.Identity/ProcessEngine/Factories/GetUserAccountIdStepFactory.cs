namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="GetUserAccountIdStep"/>.
/// </summary>
internal sealed class GetUserAccountIdStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var userService = sp.GetRequiredService<IUserService>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new GetUserAccountIdStep
        {
            Kind        = Kind,
            UserService = userService,
            Selector    = new Selector(),
            Logger      = loggerFactory.CreateLogger(nameof(GetUserAccountIdStep)),
            Next        = cfg.StrOpt("next")
        };
    }
}
