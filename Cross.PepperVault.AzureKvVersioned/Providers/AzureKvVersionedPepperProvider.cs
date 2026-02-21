namespace Cross.PepperVault.Providers;

/// <summary>
/// Провайдер «перцев» из Azure Key Vault — версии секрета (tags: pepperVersion)
/// Берёт все версии секрета <see cref="AzureKvVersionsProviderOptions.SecretName"/>,
/// фильтрует включённые (<c>Enabled == true</c>), извлекает тег с именем
/// <see cref="AzureKvVersionsProviderOptions.TagName"/> (по умолчанию <c>pepperVersion</c>)
/// как номер версии (short), получает значения каждой версии и формирует карту:
/// <c>{ version → pepper }</c>. Текущая версия — максимальная из найденных.
/// </summary>
public sealed class AzureKvVersionedPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<AzureKvVersionsProviderOptions> _options;

    /// <summary>
    /// Создаёт провайдер с поддержкой TTL-кэша и авто-перечитыванием при изменении опций.
    /// </summary>
    public AzureKvVersionedPepperProvider(IOptionsMonitor<PepperOptions<AzureKvVersionsProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<AzureKvVersionsProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override async Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        var credential = CreateCredential(_options.ProviderOptions.AzureCredential);
        var client = new SecretClient(_options.ProviderOptions.VaultUri, credential);

        var dict = new Dictionary<short, string>();
        short? current = null;

        await foreach (SecretProperties p in client.GetPropertiesOfSecretVersionsAsync(_options.ProviderOptions.SecretName, cancellationToken))
        {
            if (p.Enabled != true)
                continue;

            if (!p.Tags.TryGetValue(_options.ProviderOptions.TagName, out var verStr) ||
                !short.TryParse(verStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            {
                continue; // версия без корректного тега — игнорируем
            }

            KeyVaultSecret s;
            try
            {
                s = await client.GetSecretAsync(_options.ProviderOptions.SecretName, p.Version, cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException ex)
            {
                // Версию могли удалить/ограничить доступ — безопасно пропускаем
                continue;
            }

            var value = s.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Secret '{_options.ProviderOptions.SecretName}' version '{p.Version}' has empty value.");

            dict[v] = value;
            current = current is null ? v : (short)Math.Max(current.Value, v);
        }

        if (current is null || dict.Count == 0)
            throw new InvalidOperationException("No enabled pepper versions found in Azure Key Vault.");

        if (!dict.ContainsKey(current.Value))
            throw new InvalidOperationException($"Current pepper (v{current.Value}) missing in versions map.");

        return (current.Value, new ReadOnlyDictionary<short, string>(dict));
    }

    /// <summary>
    /// Выбор типа учётных данных Azure по строке конфигурации.
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

        // при необходимости добавь другие режимы (WorkloadIdentityCredential и т.д.)
        return new DefaultAzureCredential();
    }
}
