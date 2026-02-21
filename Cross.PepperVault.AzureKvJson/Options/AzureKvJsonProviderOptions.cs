namespace Cross.PepperVault.Options;

public sealed record AzureKvJsonProviderOptions
{
    /// <summary>
    /// https://<vault>.vault.azure.net/
    /// </summary>
    public required Uri VaultUri { get; init; }

    /// <summary>
    /// имя секрета с JSON
    /// </summary>
    public required string SecretName { get; init; }

    /// <summary>
    /// "managed:<clientId>" | null → DefaultAzureCredential
    /// </summary>
    public string? AzureCredential { get; init; }
}
