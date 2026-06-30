namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CodeAuthStep"/>.
/// <para>
/// Supported step JSON schema (example reading data from the previous <c>auth-form</c> step):
/// </para>
/// <code language="json">
/// {
///   "kind": "codeAuth",
///   "name": "code-auth",
///   "channel": "phone",
///   "identityKey": "auth-form.Phone",   // absolute key → read from the "auth-form" step
///   "codeKey":     "auth-form.Code",    // absolute key → read from the "auth-form" step
///   "resolveBy": { "field": "Phone" },
///   "userIdKey": "UserId",              // (opt.) relative → saved as "code-auth.UserId"
///   "next": "issue"                     // (opt.) null — finish the process
/// }
/// </code>
/// <remarks>
/// Key usage rules:
/// <list type="bullet">
/// <item><description>A relative key (no dot) is automatically qualified as <c>"{name}.{key}"</c>, where <c>name</c> is the current step name.</description></item>
/// <item><description>To read data from another step, specify an absolute key: <c>"{other-step}.{Field}"</c>.</description></item>
/// </list>
/// </remarks>
/// </summary>
internal sealed class CodeAuthStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // Required dependencies (from DI)
        var codeService = sp.GetRequiredService<ICodeService>();
        var userService = sp.GetRequiredService<IUserService>();

        // Required step fields
        var channel     = cfg.Str("channel");
        var identityKey = cfg.Str("identityKey");
        var codeKey     = cfg.Str("codeKey");

        // resolveBy.field
        if (!cfg.TryGetProperty("resolveBy", out var resolveEl) || resolveEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("codeAuth: 'resolveBy' object is required.");
        var field = resolveEl.Str("field");

        // Optional fields
        var userIdKey = cfg.StrOpt("userIdKey") ?? "UserId"; // relative by default → "{Name}.UserId"
        var next      = cfg.StrOpt("next");

        return new CodeAuthStep
        {
            Kind        = Kind,
            CodeService = codeService,
            UserService = userService,
            Channel     = channel,
            IdentityKey = identityKey,
            CodeKey     = codeKey,
            ResolveBy   = new ResolveBy { Field = field },
            UserIdKey   = userIdKey,
            Next        = next
        };
    }
}
