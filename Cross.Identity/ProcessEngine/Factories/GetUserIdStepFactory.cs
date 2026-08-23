namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="GetUserIdStep"/>.
/// </summary>
internal sealed class GetUserIdStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var userService = sp.GetRequiredService<IUserService>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        return new GetUserIdStep
        {
            Kind        = Kind,
            UserService = userService,
            Selector    = new Selector(),
            Logger      = loggerFactory.CreateLogger(nameof(GetUserIdStep)),
            Next        = cfg.StrOpt("next")
        };
    }
}
