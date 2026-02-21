namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» из GCP Secret Manager — один secret (latest) c JSON.
/// Секрет содержит JSON вида:
/// <code>
/// { "current": N, "peppers": { "1": "...", "2": "...", ... } }
/// </code>
/// </summary>
public sealed class GcpSecretManagerJsonPepperProvider : PepperProviderBase
{
    private readonly SecretManagerServiceClient _client;
    private readonly PepperOptions<GcpSecretManagerJsonProviderOptions> _options;

    /// <summary>
    /// Создаёт провайдер с поддержкой TTL-кэша и авто-инвалидцией при изменении опций.
    /// </summary>
    /// <param name="client">Клиент GCP Secret Manager (сконфигурируй под нужные креды).</param>
    /// <param name="monitor">Монитор опций <see cref="PepperOptions{TProviderOptions}"/>.</param>
    public GcpSecretManagerJsonPepperProvider(SecretManagerServiceClient client, IOptionsMonitor<PepperOptions<GcpSecretManagerJsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<GcpSecretManagerJsonProviderOptions>(monitor))
    {
        _client = client;
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        // Разворачиваем components из resource name
        if (!SecretName.TryParse(_options.ProviderOptions.SecretName, out var sn))
            throw new InvalidOperationException($"Invalid SecretName '{_options.ProviderOptions.SecretName}'.");

        var verName = new SecretVersionName(sn.ProjectId, sn.SecretId, "latest");

        AccessSecretVersionResponse resp;
        try
        {
            resp = await _client
                .AccessSecretVersionAsync(verName, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to access GCP secret '{_options.ProviderOptions.SecretName}' (version 'latest').", ex);
        }

        var json = resp.Payload?.Data?.ToStringUtf8();
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Secret '{_options.ProviderOptions.SecretName}' has empty payload.");

        // Разбираем JSON → (current, map)
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("peppers", out var peppersEl))
            throw new InvalidOperationException("Secret JSON must contain 'current' and 'peppers'.");

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
