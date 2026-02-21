namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Композитный провайдер: опрашивает цепочку <see cref="IProcessDefinitionProvider"/> по порядку.
/// Первый, кто вернёт JSON, — побеждает. Остальные не вызываются.
/// <para>
/// Умеет кэшировать найденные дефиниции (по ключу "flow.operation") до рестарта приложения.
/// </para>
/// </summary>
public sealed class CompositeProcessDefinitionProvider : IProcessDefinitionProvider
{
    private readonly IReadOnlyList<IProcessDefinitionProvider> _providers;
    private readonly ConcurrentDictionary<string, string> _flowCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _templateCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Создаёт композитный провайдер из последовательности дочерних провайдеров
    /// (их порядок = порядок fallback).
    /// </summary>
    public CompositeProcessDefinitionProvider(IEnumerable<IProcessDefinitionProvider> providers)
    {
        _providers = providers.ToList();
        if (_providers.Count == 0)
            throw new ArgumentException("At least one provider required.", nameof(providers));
    }

    /// <inheritdoc />
    public string GetJson(string flow, FlowOperationEnum operation)
    {
        var key = $"{flow}.{operation}".ToLowerInvariant();

        if (_flowCache.TryGetValue(key, out var cached))
            return cached;

        foreach (var p in _providers)
        {
            try
            {
                var json = p.GetJson(flow, operation);
                _flowCache[key] = json;
                return json;
            }
            catch (KeyNotFoundException)
            {
                // пробуем следующий провайдер
            }
        }

        throw new KeyNotFoundException($"Process definition not found in composite for '{key}'.");
    }


    /// <inheritdoc />
    public string GetTemplate(string name, string languageCode, string format)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        var key = $"{name}.{languageCode}.{format}".ToLowerInvariant();

        if (_templateCache.TryGetValue(key, out var cached))
            return cached;

        foreach (var p in _providers)
        {
            try
            {
                var tpl = p.GetTemplate(name, languageCode, format);
                _templateCache[key] = tpl;
                return tpl;
            }
            catch (KeyNotFoundException)
            {
                // пробуем следующий провайдер
            }
        }

        throw new KeyNotFoundException($"Template not found in composite for '{key}'.");
    }
}
