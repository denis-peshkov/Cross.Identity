namespace Cross.PepperVault.Providers;

public sealed class EnvJsonPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<EnvJsonProviderOptions> _options;

    public EnvJsonPepperProvider(IOptionsMonitor<PepperOptions<EnvJsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<EnvJsonProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        var jsonKey = _options.ProviderOptions.JsonKey;
        var json = Environment.GetEnvironmentVariable(jsonKey)
                   ?? throw new InvalidOperationException($"{jsonKey} is missing.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("peppers", out var peppersEl))
            throw new InvalidOperationException($"{jsonKey} must contain 'current' and 'peppers'.");

        var current = (short)currentEl.GetInt32();

        var peppers = new Dictionary<short, string>();
        foreach (var kv in peppersEl.EnumerateObject())
        {
            if (!short.TryParse(kv.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ver))
                throw new InvalidOperationException($"Invalid pepper version key '{kv.Name}' in {jsonKey}.");

            peppers[ver] = kv.Value.GetString()
                        ?? throw new InvalidOperationException($"Pepper value for version '{kv.Name}' is null.");
        }

        if (!peppers.ContainsKey(current))
            throw new InvalidOperationException("Current pepper missing in JSON map.");

        var ro = new ReadOnlyDictionary<short, string>(peppers);
        return Task.FromResult((current, (IReadOnlyDictionary<short, string>)ro));
    }
}
