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

        return new GetUserIdStep
        {
            Kind        = Kind,
            UserService = userService,
            Selector    = new Selector(),
            Next        = cfg.StrOpt("next")
        };
    }
}
