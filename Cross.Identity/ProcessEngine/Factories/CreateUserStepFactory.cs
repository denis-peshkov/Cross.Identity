namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CreateUserStep"/>.
/// </summary>
internal sealed class CreateUserStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // map of "user field" -> "Bag key" (may be absolute or relative)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in cfg.GetProperty("map").EnumerateObject())
            map[p.Name] = p.Value.GetString()!;

        return new CreateUserStep
        {
            Kind        = Kind,
            UserService = sp.GetRequiredService<IUserService>(),
            Map         = map,
            UserAccountIdKey   = cfg.StrOpt("userAccountIdKey") ?? "UserAccountId",
            Next        = cfg.StrOpt("next")
        };
    }
}
