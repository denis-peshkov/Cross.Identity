namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев», хранящихся в Azure Key Vault - JSON-секрета:
/// {
///   "current": 3,
///   "peppers": { "1": "...", "2": "...", "3": "..." }
/// }
/// </summary>
public sealed class AzureKvJsonPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<AzureKvJsonProviderOptions> _options;

    public AzureKvJsonPepperProvider(
        IOptionsMonitor<PepperOptions<AzureKvJsonProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<AzureKvJsonProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        var vaultUri = _options.ProviderOptions.VaultUri ?? throw new InvalidOperationException("providerOptions.vaultUri is required.");
        var secretName = _options.ProviderOptions.SecretName ?? throw new InvalidOperationException("providerOptions.secretName is required.");

        var credential = CreateCredential(_options.ProviderOptions.AzureCredential);
        var client = new SecretClient(vaultUri, credential);

        KeyVaultSecret secret;
        try
        {
            secret = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to read secret '{secretName}' from '{vaultUri}'.", ex);
        }

        var json = secret.Value;
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException($"Secret '{secretName}' value is null/empty.");

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("peppers", out var peppersEl))
            throw new InvalidOperationException("Secret JSON must contain 'current' and 'peppers' properties.");

        var current = checked((short)currentEl.GetInt32());

        var dict = new Dictionary<short, string>();
        foreach (var kv in peppersEl.EnumerateObject())
        {
            if (!short.TryParse(kv.Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ver))
                throw new InvalidOperationException($"Invalid pepper version key '{kv.Name}' (expected short).");

            var val = kv.Value.GetString();
            if (string.IsNullOrEmpty(val))
                throw new InvalidOperationException($"Pepper value for version '{kv.Name}' is null/empty.");

            dict[ver] = val;
        }

        if (!dict.ContainsKey(current))
            throw new InvalidOperationException("Current pepper is missing in 'peppers' map.");

        return (current, new ReadOnlyDictionary<short, string>(dict));
    }

    /// <summary>
    /// Простейший фабричный метод для выбора кредов.
    /// po.AzureCredential можно использовать так:
    /// - null / ""                   → DefaultAzureCredential
    /// - "managed:<clientId>"        → ManagedIdentityCredential(clientId)
    /// - "workload"                  → WorkloadIdentityCredential (если нужен)
    /// - "viamodule" и т.п.          → дополни по своему контракту
    /// </summary>
    private static TokenCredential CreateCredential(string? azureCredential)
    {
        if (string.IsNullOrWhiteSpace(azureCredential))
            return new DefaultAzureCredential();

        if (azureCredential.StartsWith("managed:", StringComparison.OrdinalIgnoreCase))
        {
            var clientId = azureCredential["managed:".Length..].Trim();
            return string.IsNullOrEmpty(clientId)
                ? new ManagedIdentityCredential()
                : new ManagedIdentityCredential(clientId: clientId);
        }

        // при необходимости добавляй другие режимы
        return new DefaultAzureCredential();
    }
}
