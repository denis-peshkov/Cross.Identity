namespace Cross.PepperVault.Options;

/// <summary>
/// Опции провайдера, читающего перцы из локального JSON-файла.
/// Формат JSON:
/// { "current": 3, "peppers": { "1": "...", "2": "...", "3": "..." } }
/// </summary>
public sealed record FileJsonProviderOptions
{
    /// <summary>Полный путь к JSON-файлу.</summary>
    public required string Path { get; init; }
}
