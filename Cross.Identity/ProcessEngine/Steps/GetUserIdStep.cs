namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг поиска пользователя и публикации его идентификатора в контекст процесса (<see cref="Bag"/>).
/// Ищет через <see cref="IUserService"/> по указанному полю (Email / UserName / Phone / ...).
/// <para>
/// Ключи:
/// <list type="bullet">
///   <item><description><see cref="SelectorKey"/> — если относительный (без точки), читается как <c>"{Name}.{SelectorKey}"</c>;</description></item>
///   <item><description><see cref="UserIdKey"/> — если относительный, записывается как <c>"{Name}.{UserIdKey}"</c>.</description></item>
///   <item><description>Чтобы взять данные из другого шага, укажи абсолютный ключ вида <c>"other-step.Field"</c>.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed class GetUserIdStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Сервис пользователей.</summary>
    public required IUserService UserService { get; init; }

    /// <summary>Имя поля для поиска: "Email" | "UserName" | "Phone" | ...</summary>
    public required string SelectorField { get; init; }

    /// <summary>
    /// Ключ в <see cref="Bag"/>, откуда взять значение селектора (напр., <c>"auth-form.Email"</c>).
    /// Может быть относительным (будет квалифицирован как <c>"{Kind}.SelectorKey"</c>) или абсолютным.
    /// </summary>
    public required string SelectorKey { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // относительный → "{Kind}.{SelectorKey}"
        var selectorValue = ctx.Get<string>(BagKey.Qualify(Kind, SelectorKey));

        var userId = await UserService.GetUserIdByAsync(SelectorField, selectorValue, cancellationToken);
        if (userId is null)
            return StepResult.Fail(new KeyNotFoundException("User not found."));

        // относительный → "{Kind}.{UserIdKey}"
        ctx.Set(BagKey.Qualify(Kind, "UserId"), userId);

        return StepResult.Ok(Next);
    }
}
