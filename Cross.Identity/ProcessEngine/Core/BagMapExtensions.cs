namespace Cross.Identity.ProcessEngine.Core;

public static class BagMapExtensions
{
    /// <summary>
    /// Converts public object properties to a &lt;name, value&gt; dictionary,
    /// including only simple types. The key name comes from <see cref="JsonPropertyNameAttribute"/> when set.
    /// Properties with <see cref="JsonIgnoreAttribute"/> are skipped.
    /// </summary>
    /// <param name="source">Source object.</param>
    /// <param name="includeNulls">Whether to include properties with a <c>null</c> value.</param>
    /// <param name="enumAsString">Whether to serialize an enum as a string (name) instead of a numeric value.</param>
    public static Dictionary<string, object?> ToBag(
        this object source,
        bool includeNulls = false,
        bool enumAsString = true)
    {
        ArgumentNullException.ThrowIfNull(source);

        var type = source.GetType();
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // skip properties without a getter or indexers
            if (prop.GetMethod is null || prop.GetIndexParameters().Length > 0)
                continue;

            // [JsonIgnore] — skip
            if (prop.IsDefined(typeof(JsonIgnoreAttribute), inherit: true))
                continue;

            var propType = prop.PropertyType;
            var (isSimple, underlying) = IsSimpleType(propType);
            if (!isSimple)
                continue;

            var value = prop.GetValue(source);

            if (value is null && !includeNulls)
                continue;

            // Trim string values
            if (underlying == typeof(string))
            {
                value = value?.ToString()?.Trim();
            }

            // Key: JsonPropertyName or property name
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true)?.Name;
            var key = string.IsNullOrWhiteSpace(jsonName) ? prop.Name : jsonName;

            // enum as string/number
            if (value is not null && (underlying?.IsEnum ?? propType.IsEnum))
            {
                value = enumAsString
                    ? value.ToString()
                    : Convert.ChangeType(value, Enum.GetUnderlyingType(underlying ?? propType), CultureInfo.InvariantCulture);
            }

            dict[key] = value;
        }

        return dict;
    }

    /// <summary>
    /// Create object T from a dictionary of simple values, honoring JsonPropertyName/JsonIgnore.
    /// Only simple types are supported (string/numbers/bool/decimal/Guid/DateTime/…/enum/Nullable).
    /// Dictionary keys are compared case-insensitively.
    /// </summary>
    public static T FromBag<T>(
        this IDictionary<string, object?> dict,
        bool enumFromString = true) where T : new()
    {
        ArgumentNullException.ThrowIfNull(dict);

        // case-insensitive access
        var src = dict is Dictionary<string, object?> d && d.Comparer.Equals(StringComparer.OrdinalIgnoreCase)
            ? dict
            : new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);

        var obj = new T();
        foreach (var prop in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.SetMethod is null || prop.GetIndexParameters().Length > 0) continue;
            if (prop.IsDefined(typeof(JsonIgnoreAttribute), inherit: true)) continue;

            var (isSimple, _) = IsSimpleType(prop.PropertyType);
            if (!isSimple) continue;

            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true)?.Name ?? prop.Name;

            if (!src.TryGetValue(jsonName, out var value)) continue;
            if (value is null) { prop.SetValue(obj, null); continue; }

            var converted = ConvertSimple(value, prop.PropertyType, enumFromString);
            prop.SetValue(obj, converted);
        }

        return obj;
    }

    /// <summary>
    /// Checks whether a type is "simple" for our mapping.
    /// Supported: string, numbers, bool, decimal, DateTime, DateTimeOffset, TimeSpan, Guid,
    /// DateOnly/TimeOnly (when available), enum, and Nullable&lt;T&gt; over these types.
    /// </summary>
    private static (bool IsSimple, Type? Underlying) IsSimpleType(Type t)
    {
        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(t);
        var core = underlying ?? t;

        if (core.IsEnum) return (true, core);

        if (core == typeof(string)) return (true, core);
        if (core == typeof(bool)) return (true, core);

        if (core == typeof(byte) || core == typeof(sbyte) ||
            core == typeof(short) || core == typeof(ushort) ||
            core == typeof(int)  || core == typeof(uint)  ||
            core == typeof(long) || core == typeof(ulong) ||
            core == typeof(float) || core == typeof(double) ||
            core == typeof(decimal))
            return (true, core);

        if (core == typeof(DateTime) || core == typeof(DateTimeOffset) ||
            core == typeof(TimeSpan)  || core == typeof(Guid))
            return (true, core);

#if NET6_0_OR_GREATER
        if (core == typeof(DateOnly) || core == typeof(TimeOnly))
            return (true, core);
#endif

        return (false, null);
    }

    private static object? ConvertSimple(object value, Type targetType, bool enumFromString)
    {
        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        var core = underlying ?? targetType;

        if (value is null) return null;

        // already the target type
        if (core.IsInstanceOfType(value)) return value;

        // enum
        if (core.IsEnum)
        {
            if (enumFromString && value is string s)
                return Enum.Parse(core, s, ignoreCase: true);

            // number -> enum
            var num = Convert.ChangeType(value, Enum.GetUnderlyingType(core));
            return Enum.ToObject(core, num!);
        }

        // Guid from string
        if (core == typeof(Guid) && value is string gs)
            return Guid.Parse(gs);

        // DateTime/DateTimeOffset/TimeSpan from string
        if (core == typeof(DateTime) && value is string dts)
            return DateTime.Parse(dts, null, DateTimeStyles.RoundtripKind);
        if (core == typeof(DateTimeOffset) && value is string dto)
            return DateTimeOffset.Parse(dto, null, DateTimeStyles.RoundtripKind);
        if (core == typeof(TimeSpan) && value is string ts)
            return TimeSpan.Parse(ts, CultureInfo.InvariantCulture);

#if NET6_0_OR_GREATER
        if (core == typeof(DateOnly) && value is string dos)
            return DateOnly.Parse(dos);
        if (core == typeof(TimeOnly) && value is string tos)
            return TimeOnly.Parse(tos);
#endif

        // other simple types via Convert.ChangeType
        var converted = Convert.ChangeType(value, core);

        // wrap in Nullable<T> when needed
        return underlying is null ? converted : converted;
    }
}
