namespace Cross.Identity.ProcessEngine.Definitions.Helpers;

/// <summary>Utilities for reading values from <see cref="JsonElement"/>.</summary>
public static class JsonHelpers
{
    /// <summary>Get a required string property.</summary>
    public static string Str(this JsonElement e, string name)
        => e.GetProperty(name).GetString()!;

    /// <summary>Get a required enum property.</summary>
    public static T EnumReq<T>(this JsonElement e, string name) where T : struct, Enum
    {
        var str = e.GetProperty(name).GetString();
        if (string.IsNullOrWhiteSpace(str) || !Enum.TryParse<T>(str, ignoreCase: true, out var result))
            throw new InvalidOperationException($"Property '{name}' must be a valid {typeof(T).Name}.");
        return result;
    }

    public static T? EnumOpt<T>(this JsonElement e, string name) where T : struct, Enum
    {
        if (!e.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.String)
            return null;

        var str = p.GetString();
        if (string.IsNullOrEmpty(str))
            return null;

        return Enum.TryParse<T>(str, ignoreCase: true, out var result)
            ? result
            : null;
    }

    /// <summary>Get an optional string property.</summary>
    public static string? StrOpt(this JsonElement e, string name)
        => e.TryGetProperty(name, out var p)
            ? p.GetString()
            : null;

    /// <summary>Get an optional lifetime in seconds and convert it to <see cref="TimeSpan"/>.</summary>
    public static TimeSpan? TimeSpanSecondsOpt(this JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number
            ? TimeSpan.FromSeconds(p.GetDouble())
            : (TimeSpan?)null;

    public static bool? BoolOpt(this JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.True
            ? true
            : e.TryGetProperty(name, out p) && p.ValueKind == JsonValueKind.False
                ? false
                : (bool?)null;

    public static JsonElement Obj(this JsonElement e, string name)
    {
        if (!e.TryGetProperty(name, out var p) || p.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Property '{name}' object is required.");
        return p;
    }
}
