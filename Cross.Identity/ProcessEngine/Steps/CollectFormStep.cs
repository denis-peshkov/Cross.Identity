namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг сбора данных формы.
/// <list type="bullet">
/// <item>Читает входные данные из <see cref="FetchIncoming"/> (обычно это <see cref="IRequestInput.GetAsync"/>).</item>
/// <item>Валидирует их через <see cref="Validator"/> (FluentValidation).</item>
/// <item>Сохраняет значения в <see cref="Bag"/> с префиксом <b>имени шага</b>: <c>"{Name}.{FieldKey}"</c>.</item>
/// </list>
/// Схема формы задаётся в конфигурации шага (см. фабрику).
/// </summary>
internal sealed class CollectFormStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; }

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>Схема формы.</summary>
    public required FormSchema Schema { get; init; }

    /// <summary>Валидатор формы (FluentValidation), генерируется фабрикой.</summary>
    public required IValidator<IDictionary<string, object?>> Validator { get; init; }

    /// <summary>Функция получения входных данных (обычно из <see cref="IRequestInput"/>).</summary>
    public required Func<CancellationToken, Task<IDictionary<string, object?>>> FetchIncoming { get; init; }

    /// <inheritdoc />
    public async ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        // 1) входные данные
        var data = await FetchIncoming(cancellationToken);

        // 2) валидация
        var res = await Validator.ValidateAsync(data, cancellationToken);
        if (!res.IsValid)
            return StepResult.Fail(new ValidationException(res.Errors));

        // 3) запись в Bag с префиксом имени шага
        foreach (var (k, v) in data)
        {
            var bagKey = BagKey.Qualify(Kind, k); // "{Kind}.{k}" если ключ относительный
            ctx.Set(bagKey, v);
        }

        return StepResult.Ok(Next);
    }
}
