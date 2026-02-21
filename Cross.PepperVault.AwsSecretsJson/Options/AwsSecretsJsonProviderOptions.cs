namespace Cross.PepperVault.Options;

/// <summary>
/// Опции провайдера AWS Secrets Manager (один secret с JSON).
/// </summary>
public sealed record AwsSecretsJsonProviderOptions
{
    /// <summary>
    /// Идентификатор секрета в AWS Secrets Manager (name/ARN).
    /// Пример: "auth/peppers/json" или полный ARN.
    /// </summary>
    public required string SecretId { get; init; }
}
