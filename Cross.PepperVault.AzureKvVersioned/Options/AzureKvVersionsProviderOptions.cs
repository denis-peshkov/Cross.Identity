namespace Cross.PepperVault.Options;

/// <summary>
/// Опции провайдера Azure Key Vault (режим по версиям секрета).
/// Каждая версия секрета <c>SecretName</c> должна иметь тег <c>TagName</c>
/// со значением номера версии перца (short), например: <c>pepperVersion=3</c>.
/// Текущая версия выбирается как максимальный номер среди включённых версий.
/// </summary>
public sealed record AzureKvVersionsProviderOptions
{
    /// <summary>URI хранилища: <c>https://&lt;vault&gt;.vault.azure.net/</c>.</summary>
    public required Uri VaultUri { get; init; }

    /// <summary>Имя секрета в Azure Key Vault, у которого используются версии.</summary>
    public required string SecretName { get; init; }

    /// <summary>
    /// Имя тега, в котором хранится номер версии перца. По умолчанию: <c>"pepperVersion"</c>.
    /// </summary>
    public string TagName { get; init; } = "pepperVersion";

    /// <summary>
    /// Способ аутентификации к Azure:
    /// <list type="bullet">
    /// <item><description><c>null</c> или пусто → <see cref="Azure.Identity.DefaultAzureCredential"/></description></item>
    /// <item><description><c>"managed:&lt;clientId&gt;"</c> → <see cref="Azure.Identity.ManagedIdentityCredential"/></description></item>
    /// </list>
    /// </summary>
    public string? AzureCredential { get; init; }
}
