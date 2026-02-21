namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="VerifyCodeStep"/>.
/// JSON-параметры:
/// <list type="bullet">
/// <item><description><c>name</c> — имя шага;</description></item>
/// <item><description><c>channel</c> — канал проверки (например, "email" или "phone");</description></item>
/// <item><description><c>identityKey</c> — ключ в Bag для идентификатора (email/phone/username);
///     если относительный (без точки) → читается как <c>"{name}.identityKey"</c>;
///     если абсолютный (с точкой) — используется как есть.</description></item>
/// <item><description><c>codeKey</c> — ключ в Bag для проверочного кода;
///     работает по тем же правилам (относительный/абсолютный).</description></item>
/// <item><description><c>next</c> — (опц.) имя следующего шага; <c>null</c> — завершить процесс.</description></item>
/// </list>
/// </summary>
internal sealed class VerifyCodeStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        var codeService = sp.GetRequiredService<ICodeService>();

        return new VerifyCodeStep
        {
            Kind        = Kind,
            Channel     = cfg.Str("channel"),
            IdentityKey = cfg.Str("identityKey"),
            CodeKey     = cfg.Str("codeKey"),
            CodeService = codeService,
            Next        = cfg.StrOpt("next")
        };
    }
}
