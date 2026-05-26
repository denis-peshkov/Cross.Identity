namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Хелпер для квалификации ключей Bag:
/// если ключ относительный (без точки) — возвращает "{stepName}.{key}",
/// если абсолютный (с точкой) — возвращает как есть.
/// </summary>
internal static class BagKey
{
    public static string Qualify(string stepName, string key)
    {
        if (string.IsNullOrWhiteSpace(stepName))
            throw new ArgumentException("Step name must be provided.", nameof(stepName));
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key must be provided.", nameof(key));

        return key.Contains('.', StringComparison.Ordinal) ? key : $"{stepName}.{key}";
    }
}
