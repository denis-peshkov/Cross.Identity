namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» из HashiCorp Vault (KV v2), где секрет хранится как JSON:
/// { "current": N, "peppers": { "1": "...", ... } }.
/// </summary>
public sealed class HcvKv2JsonPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<HcvKv2JsonProviderOptions> _options;

    /// <summary>
    /// Создаёт провайдер с поддержкой TTL-кэша и авто-инвалидцией при изменении опций.
    /// </summary>
    public HcvKv2JsonPepperProvider(IOptionsMonitor<PepperOptions<HcvKv2JsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<HcvKv2JsonProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        var settings = new VaultClientSettings(
            _options.ProviderOptions.VaultUri,
            new TokenAuthMethodInfo(_options.ProviderOptions.Token))
        {
            // сюда можно добавить настройки HttpClientFactory, таймауты, политики ретраев и т.п.
        };
        var vault = new VaultClient(settings);

        // KV v2: читаем секрет (Data.Data — словарь user-полей, который у нас — JSON-объект)
        var resp = await vault.V1.Secrets.KeyValue.V2
            .ReadSecretAsync(path: _options.ProviderOptions.Path, mountPoint: _options.ProviderOptions.Mount)
            .ConfigureAwait(false);

        // Преобразуем словарь в JSON-строку, затем парсим как единый объект со схемой current/peppers
        var json = JsonSerializer.Serialize(resp.Data.Data);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("peppers", out var peppersEl))
            throw new InvalidOperationException("Vault JSON must contain 'current' and 'peppers'.");

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
