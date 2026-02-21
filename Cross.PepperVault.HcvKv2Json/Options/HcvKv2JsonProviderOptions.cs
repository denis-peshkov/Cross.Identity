namespace Cross.PepperVault.Options;

/// <summary>
/// Опции провайдера HashiCorp Vault (KV v2), где значение секрета — JSON-объект вида:
/// { "current": 3, "peppers": { "1": "...", "2": "...", "3": "..." } }
/// </summary>
public sealed record HcvKv2JsonProviderOptions
{
    /// <summary>Базовый URI Vault, например: https://vault.example.com:8200</summary>
    public required string VaultUri { get; init; }

    /// <summary>Токен доступа Vault (Token auth). Если хочешь другие методы — расширим.</summary>
    public required string Token { get; init; }

    /// <summary>Точка монтирования движка KV v2, например: "secret".</summary>
    public required string Mount { get; init; }

    /// <summary>Путь к секрету внутри mount, например: "auth/peppers".</summary>
    public required string Path { get; init; }
}
