namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Helper for qualifying Bag keys:
/// if the key is relative (no dot), returns "{stepName}.{key}",
/// if absolute (with a dot), returns it as-is.
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
