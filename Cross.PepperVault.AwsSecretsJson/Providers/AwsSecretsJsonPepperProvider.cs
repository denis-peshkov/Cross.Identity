namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» из AWS Secrets Manager - один secret с JSON:
/// <code>
/// {
///   "current": 3,
///   "peppers": { "1": "...", "2": "...", "3": "..." }
/// }
/// </code>
/// </summary>
public sealed class AwsSecretsJsonPepperProvider : PepperProviderBase
{
    private readonly IAmazonSecretsManager _sm;
    private readonly PepperOptions<AwsSecretsJsonProviderOptions> _options;

    /// <summary>
    /// Создаёт провайдер с поддержкой TTL-кэша и авто-инвалидцией при изменении опций.
    /// </summary>
    public AwsSecretsJsonPepperProvider(IAmazonSecretsManager sm, IOptionsMonitor<PepperOptions<AwsSecretsJsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<AwsSecretsJsonProviderOptions>(monitor))
    {
        _sm = sm;
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        GetSecretValueResponse resp;
        try
        {
            resp = await _sm.GetSecretValueAsync(new GetSecretValueRequest { SecretId = _options.ProviderOptions.SecretId }, cancellationToken).ConfigureAwait(false);
        }
        catch (ResourceNotFoundException ex)
        {
            throw new InvalidOperationException($"AWS secret '{_options.ProviderOptions.SecretId}' not found.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read AWS secret '{_options.ProviderOptions.SecretId}'.", ex);
        }

        var json = !string.IsNullOrEmpty(resp.SecretString)
            ? resp.SecretString
            : resp.SecretBinary is { Length: > 0 }
                ? Encoding.UTF8.GetString(resp.SecretBinary.ToArray())
                : null;

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Secret '{_options.ProviderOptions.SecretId}' has empty value (both string and binary).");

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
