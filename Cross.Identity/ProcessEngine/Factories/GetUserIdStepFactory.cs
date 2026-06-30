namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="GetUserIdStep"/>.
/// JSON parameters:
/// <list type="bullet">
/// <item><description><c>name</c> — step name;</description></item>
/// <item><description><c>selectorField</c> — lookup field ("Email" | "UserName" | "Phone" | ...);</description></item>
/// <item><description><c>selectorKey</c> — Bag key to read the selector value from:
///   relative (no dot) → will be read as <c>"{name}.selectorKey"</c>,
///   absolute (with a dot) is used as-is;</description></item>
/// <item><description><c>userIdKey</c> — (opt.) key for storing the result; relative <c>"UserId"</c> by default
///   → will be saved as <c>"{name}.UserId"</c>;</description></item>
/// <item><description><c>next</c> — (opt.) next step name, <c>null</c> — finish.</description></item>
/// </list>
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
            Kind          = Kind,
            UserService   = userService,
            SelectorField = cfg.Str("selectorField"),
            SelectorKey   = cfg.Str("selectorKey"),
            Next          = cfg.StrOpt("next")
        };
    }
}
