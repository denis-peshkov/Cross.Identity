namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Реестр фабрик шагов. Позволяет создать шаг по его <c>kind</c>.
/// <para>
/// Потокобезопасность: предполагается инициализация на старте приложения (DI),
/// после чего только чтение. Если нужно регистрировать во время работы — оборачивайте синхронизацией.
/// </para>
/// </summary>
internal sealed class StepRegistry
{
    private readonly Dictionary<string, IStepFactory> _steps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Создать пустой реестр.</summary>
    public StepRegistry() { }

    /// <summary>Создать реестр и зарегистрировать набор фабрик.</summary>
    public StepRegistry(IEnumerable<IStepFactory> factories)
    {
        foreach (var f in factories)
            Register(f);
    }

    /// <summary>Зарегистрировать фабрику. Последняя запись с тем же <c>Kind</c> перезапишет предыдущую.</summary>
    public void Register(IStepFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (string.IsNullOrWhiteSpace(factory.Kind))
            throw new ArgumentException("Factory.Kind must be a non-empty string.", nameof(factory));

        _steps[factory.Kind] = factory;
    }

    /// <summary>Массовая регистрация фабрик.</summary>
    public void RegisterRange(IEnumerable<IStepFactory> factories)
    {
        foreach (var f in factories)
            Register(f);
    }

    /// <summary>
    /// Создать шаг заданного типа <paramref name="kind"/>.
    /// <para>
    /// Внимание: фабрика сама проставит <c>step.Kind = factory.Kind</c>.
    /// Любое поле <c>kind</c> в JSON используется только для маршрутизации к фабрике/валидации
    /// и не должно сетиться в шаг.
    /// </para>
    /// </summary>
    public IStep Create(string kind, JsonElement cfg, IServiceProvider sp)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        ArgumentNullException.ThrowIfNull(sp);

        if (!_steps.TryGetValue(kind, out var factory))
            throw new InvalidOperationException($"Unknown step kind '{kind}'.");

        // фабрика внутри может дополнительно валидировать, что cfg.kind (если указан) совпадает с factory.Kind
        return factory.Create(cfg, sp);
    }

    /// <summary>
    /// Создать шаг напрямую из JSON-конфига шага, извлекая <c>kind</c> из <paramref name="cfg"/>.
    /// </summary>
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        if (!cfg.TryGetProperty("kind", out var k) || k.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Step 'kind' is required in step JSON.");

        var kind = k.GetString()!;

        return Create(kind, cfg, sp);
    }

    /// <summary>Проверить, зарегистрирован ли фабрика для указанного <paramref name="kind"/>.</summary>
    public bool Has(string kind)
        => _steps.ContainsKey(kind);

    /// <summary>Список доступных <c>kind</c>.</summary>
    public IReadOnlyCollection<string> Kinds
        => _steps.Keys.ToArray();

    /// <summary>Очистить реестр (использовать осторожно).</summary>
    public void Clear()
        => _steps.Clear();
}
