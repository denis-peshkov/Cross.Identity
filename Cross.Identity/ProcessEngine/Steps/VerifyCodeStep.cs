namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг проверки кода подтверждения (email/phone).
/// Использует <see cref="ICodeService"/> для валидации кода.
/// <para>
/// Правила ключей:
/// <list type="bullet">
///   <item><description><see cref="IdentityKey"/> и <see cref="CodeKey"/> — если относительные (без точки),
///     читаются как <c>"{Name}.{Key}"</c>; чтобы читать данные из другого шага, укажи абсолютные ключи
///     вида <c>"other-step.Field"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class VerifyCodeStep : IStep
{
    /// <inheritdoc/>
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Канал верификации: "email" или "phone".</summary>
    public required string Channel { get; init; }

    /// <summary>
    /// Ключ идентификатора (email/phone/username) в <see cref="Bag"/>.
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.IdentityKey"</c>) или абсолютным.
    /// </summary>
    public required string IdentityKey { get; init; }

    /// <summary>
    /// Ключ проверочного кода в <see cref="Bag"/>.
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.CodeKey"</c>) или абсолютным.
    /// </summary>
    public required string CodeKey { get; init; }

    /// <summary>Сервис кодов.</summary>
    public required ICodeService CodeService { get; init; }

    /// <inheritdoc/>
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // относительные ключи → "{Kind}.{Key}"
        var identity = ctx.Get<string>(BagKey.Qualify(Kind, IdentityKey));
        var code     = ctx.Get<string>(BagKey.Qualify(Kind, CodeKey));

        var ok = await CodeService.VerifyAsync(Channel, identity, code, cancellationToken);

        if (!ok)
            return StepResult.Fail(new NotAuthorizedException("Invalid or expired verification code."));

        return StepResult.Ok(Next);
    }
}
