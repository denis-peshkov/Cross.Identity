namespace Cross.PepperVault.Options;

/// <summary>
/// Опции провайдера GCP Secret Manager (один secret с JSON, читается версия "latest").
/// </summary>
public sealed record GcpSecretManagerJsonProviderOptions
{
    /// <summary>
    /// Полное имя секрета в формате <c>projects/{projectId}/secrets/{secretId}</c>.
    /// Пример: <c>projects/my-proj/secrets/auth-peppers</c>.
    /// </summary>
    public required string SecretName { get; init; }
}
