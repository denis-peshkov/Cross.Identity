namespace Cross.Identity.ProcessEngine.Core;

public static class BagMapExtensions
{
    /// <summary>
    /// Преобразует публичные свойства объекта в словарь вида &lt;имя, значение&gt;,
    /// беря только простые типы. Имя ключа берётся из <see cref="JsonPropertyNameAttribute"/>, если оно задано.
    /// Свойства с <see cref="JsonIgnoreAttribute"/> пропускаются.
    /// </summary>
    /// <param name="source">Объект-источник.</param>
    /// <param name="includeNulls">Включать ли свойства со значением <c>null</c>.</param>
    /// <param name="enumAsString">Сериализовать ли enum как строку (имя) вместо числового значения.</param>
    public static Dictionary<string, object?> ToBag(
        this object source,
        bool includeNulls = false,
        bool enumAsString = true)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));

        var type = source.GetType();
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            // пропускаем без getter-а или indexer-ы
            if (prop.GetMethod is null || prop.GetIndexParameters().Length > 0)
                continue;

            // [JsonIgnore] — пропустить
            if (prop.IsDefined(typeof(JsonIgnoreAttribute), inherit: true))
                continue;

            var propType = prop.PropertyType;
            var (isSimple, underlying) = IsSimpleType(propType);
            if (!isSimple)
                continue;

            var value = prop.GetValue(source);

            if (value is null && !includeNulls)
                continue;

            // Ключ: JsonPropertyName или имя свойства
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>(inherit: true)?.Name;
            var key = string.IsNullOrWhiteSpace(jsonName) ? prop.Name : jsonName;

            // enum как строка/число
            if (value is not null && (underlying?.IsEnum ?? propType.IsEnum))
            {
                value = enumAsString ? value.ToString() : Convert.ChangeType(value, Enum.GetUnderlyingType(underlying ?? propType));
            }

            dict[key] = value;
        }

        return dict;
    }

    /// <summary>
    /// Создать объект T из словаря простых значений, учитывая JsonPropertyName/JsonIgnore.
    /// Берёт только простые типы (string/числа/bool/decimal/Guid/DateTime/…/enum/Nullable).
    /// Ключи словаря сравниваются без учета регистра.
    /// </summary>
    public static T FromBag<T>(
        this IDictionary<string, object?> dict,
        bool enumFromString = true) where T : new()
    {
        if (dict is null) throw new ArgumentNullException(nameof(dict));

        // делаем регистронезависимый доступ
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
    /// Проверяет, является ли тип "простым" для нашего маппинга.
    /// Поддерживаются: string, числа, bool, decimal, DateTime, DateTimeOffset, TimeSpan, Guid,
    /// DateOnly/TimeOnly (если доступны), enum и Nullable&lt;T&gt; над этими типами.
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

        // уже нужного типа
        if (core.IsInstanceOfType(value)) return value;

        // enum
        if (core.IsEnum)
        {
            if (enumFromString && value is string s)
                return Enum.Parse(core, s, ignoreCase: true);

            // число -> enum
            var num = Convert.ChangeType(value, Enum.GetUnderlyingType(core));
            return Enum.ToObject(core, num!);
        }

        // Guid из строки
        if (core == typeof(Guid) && value is string gs)
            return Guid.Parse(gs);

        // DateTime/DateTimeOffset/TimeSpan из строки
        if (core == typeof(DateTime) && value is string dts)
            return DateTime.Parse(dts, null, DateTimeStyles.RoundtripKind);
        if (core == typeof(DateTimeOffset) && value is string dto)
            return DateTimeOffset.Parse(dto, null, DateTimeStyles.RoundtripKind);
        if (core == typeof(TimeSpan) && value is string ts)
            return TimeSpan.Parse(ts);

#if NET6_0_OR_GREATER
        if (core == typeof(DateOnly) && value is string dos)
            return DateOnly.Parse(dos);
        if (core == typeof(TimeOnly) && value is string tos)
            return TimeOnly.Parse(tos);
#endif

        // прочие простые через Convert.ChangeType
        var converted = Convert.ChangeType(value, core);

        // оборачиваем в Nullable<T>, если нужно
        return underlying is null ? converted : converted;
    }
}
