namespace Cross.Identity.ProcessEngine.Definitions.Helpers;

/// <summary>Утилиты для удобного чтения значений из <see cref="JsonElement"/>.</summary>
public static class JsonHelpers
{
    /// <summary>Получить обязательное строковое свойство.</summary>
    public static string Str(this JsonElement e, string name)
        => e.GetProperty(name).GetString()!;

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

    /// <summary>Получить опциональное строковое свойство.</summary>
    public static string? StrOpt(this JsonElement e, string name)
        => e.TryGetProperty(name, out var p)
            ? p.GetString()
            : null;

    /// <summary>Получить опциональное время жизни в секундах и преобразовать в <see cref="TimeSpan"/>.</summary>
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
