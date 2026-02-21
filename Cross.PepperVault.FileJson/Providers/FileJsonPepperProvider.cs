namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» из локального JSON-файла.
/// Формат: { "current": N, "peppers": { "1": "...", ... } }.
/// </summary>
public sealed class FileJsonPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<FileJsonProviderOptions> _options;

    public FileJsonPepperProvider(IOptionsMonitor<PepperOptions<FileJsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<FileJsonProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        string json;
        try
        {
            json = await File.ReadAllTextAsync(_options.ProviderOptions.Path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read peppers file '{_options.ProviderOptions.Path}'.", ex);
        }

        return ParsePepperJson(json);
    }

    /// <summary>Парсинг JSON со схемой current/peppers.</summary>
    private static (short current, IReadOnlyDictionary<short, string> peppers) ParsePepperJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("peppers", out var peppersEl))
            throw new InvalidOperationException("File JSON must contain 'current' and 'peppers'.");

        var current = checked((short)currentEl.GetInt32());

        var peppers = new Dictionary<short, string>();
        foreach (var kv in peppersEl.EnumerateObject())
        {
            if (!short.TryParse(kv.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                throw new InvalidOperationException($"Invalid pepper version key '{kv.Name}' (expected short).");

            var val = kv.Value.GetString();
            if (string.IsNullOrEmpty(val))
                throw new InvalidOperationException($"Pepper value for version '{kv.Name}' is null/empty.");

            peppers[v] = val;
        }

        if (!peppers.ContainsKey(current))
            throw new InvalidOperationException($"Current pepper (v{current}) missing in 'peppers' map.");

        return (current, new ReadOnlyDictionary<short, string>(peppers));
    }
}
