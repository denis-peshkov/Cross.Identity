namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Фабрика шага <see cref="CollectResultStep"/>.
/// JSON-параметры:
/// <list type="bullet">
///   <item><description><c>kind</c> — должен быть <c>"collectResult"</c>.</description></item>
///   <item><description><c>map</c> — объект с проекциями:
///       <c>"поле_в_результате": "ключ_в_Bag"</c>. Примеры ключей:
///       абсолютный <c>"issueJwt.Token"</c> или относительный <c>"Token"</c> (будет прочитан как <c>"collectResult.Token"</c>).</description></item>
///   <item><description><c>resultKey</c> — (опц.) куда сохранить итоговый словарь; по умолчанию относительный <c>"Result"</c>
///       → будет записан как <c>"collectResult.Result"</c>.</description></item>
///   <item><description><c>next</c> — (опц.) имя следующего шага; <c>null</c> — завершить.</description></item>
/// </list>
/// Пример:
/// <code language="json">
/// {
///   "kind": "collectResult",
///   "map": {
///     "userId": "codeAuth.UserId",
///     "token":  "issueJwt.Token"
///   },
///   "next": null
/// }
/// </code>
/// </summary>
internal sealed class CollectResultStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // map обязателен и должен быть объектом
        if (!cfg.TryGetProperty("map", out var mapEl) || mapEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("collectResult: 'map' object is required.");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in mapEl.EnumerateObject())
        {
            // p.Name   -> поле в результате
            // p.Value  -> ключ в Bag (string)
            if (p.Value.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"collectResult: map['{p.Name}'] must be a string (bag key).");

            map[p.Name] = p.Value.GetString()!;
        }

        var returnEmpty = cfg.TryGetProperty("returnEmpty", out var re) && re.ValueKind == JsonValueKind.True;

        return new CollectResultStep
        {
            Kind        = Kind,
            Map         = map,
            ReturnEmpty = returnEmpty,
            Next        = cfg.StrOpt("next")
        };
    }
}
