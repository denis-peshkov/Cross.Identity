namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Small utility for validating the <c>kind</c> field in a step JSON config.
/// </summary>
internal static class StepFactoryJsonGuards
{
    public static void ValidateOptionalKind(JsonElement cfg, string expectedKind)
    {
        if (cfg.TryGetProperty("kind", out var k) && k.ValueKind == JsonValueKind.String)
        {
            var actual = k.GetString();
            if (!string.Equals(actual, expectedKind, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Step kind mismatch. Expected '{expectedKind}', but got '{actual}'.");
        }
    }
}
