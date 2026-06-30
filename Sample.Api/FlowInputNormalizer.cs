namespace Sample.Api;

/// <summary>
/// Converts Minimal API JSON body values (<see cref="JsonElement"/>) to CLR types for <see cref="IFlowExecutor"/>.
/// </summary>
internal static class FlowInputNormalizer
{
    public static Dictionary<string, object?> Normalize(Dictionary<string, object?> body)
    {
        var result = new Dictionary<string, object?>(body.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in body)
        {
            result[key] = NormalizeValue(value);
        }

        return result;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is not JsonElement element)
        {
            return value;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var integer) ? integer : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(NormalizeElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => NormalizeElement(p.Value), StringComparer.OrdinalIgnoreCase),
            _ => element.GetRawText(),
        };
    }

    private static object? NormalizeElement(JsonElement element) => NormalizeValue(element);
}
