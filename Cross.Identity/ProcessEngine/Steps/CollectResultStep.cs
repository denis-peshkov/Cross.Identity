namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Шаг агрегации результата из <see cref="Bag"/> по карте полей и публикации
/// итогового словаря в один ключ (по умолчанию — <c>"{Kind}.Result"</c>).
/// <para>
/// Никакой валидации не выполняет: отсутствующие значения просто пропускаются.
/// </para>
/// Пример использования (логика см. фабрику):
/// <code language="json">
/// {
///   "kind": "collectResult",
///   "map": {
///     "userId": "codeAuth.UserId",
///     "token":  "issueJwt.Token"
///   },
///   "next": null
/// }
/// Пример результата:
/// "collectResult.userId", "collectResult.token".
/// </code>
/// </summary>
internal sealed class CollectResultStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; } = "collectResult";

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>
    /// Карта "имя поля в результате" → "ключ в <see cref="Bag"/>".
    /// Значение-ключ может быть абсолютным (<c>"step.Field"</c>) или относительным
    /// (тогда будет квалифицирован как <c>"{Kind}.Field"</c>).
    /// </summary>
    public required IReadOnlyDictionary<string, string> Map { get; init; }

    /// <summary>
    /// Если true — шаг явно указывает, что данных для возврата нет
    /// (FlowExecutor вернёт Data = null).
    /// </summary>
    public bool ReturnEmpty { get; init; }

    /// <inheritdoc />
    public ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        if (ReturnEmpty)
        {
            ctx.Set($"{Kind}._empty", true);
            return ValueTask.FromResult(StepResult.Ok(Next));
        }

        foreach (var (outField, bagKeyRaw) in Map)
        {
            // Относительный ключ → "{Kind}.{bagKeyRaw}"
            var bagKey = BagKey.Qualify(Kind, bagKeyRaw);

            if (ctx.TryGet<object?>(bagKey, out var value))
            {
                ctx.Set($"{Kind}.{outField}", value);
            }
            else
            {
                // без валидации: просто пропустим отсутствующие ключи
                // Если нужно класть null — раскомментируй:
                // output[outField] = null;
            }
        }

        return ValueTask.FromResult(StepResult.Ok(Next));
    }
}
