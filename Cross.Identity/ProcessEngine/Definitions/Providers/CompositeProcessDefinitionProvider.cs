namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Composite provider: queries a chain of <see cref="IProcessDefinitionProvider"/> instances in order.
/// The first provider that returns JSON wins. The rest are not called.
/// <para>
/// Can cache found definitions (by key "flow.operation") until application restart.
/// </para>
/// </summary>
internal sealed class CompositeProcessDefinitionProvider : IProcessDefinitionProvider
{
    private readonly IReadOnlyList<IProcessDefinitionProvider> _providers;
    private readonly ConcurrentDictionary<string, string> _flowCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _templateCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a composite provider from a sequence of child providers
    /// (their order = fallback order).
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
                // try the next provider
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
                // try the next provider
            }
        }

        throw new KeyNotFoundException($"Template not found in composite for '{key}'.");
    }
}
