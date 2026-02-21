namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Простой property-bag для обмена данными между шагами процесса.
/// Ключи рекомендуются неймспейсить: "registration.Email", "auth.Phone", "user.Id", "auth.Token".
/// </summary>
public sealed class Bag : IReadOnlyDictionary<string, object?>
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.Ordinal);

    /// <summary>Получить значение по ключу с приведением типа.</summary>
    public T Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        // точное приведение, либо через Convert.ChangeType для примитивов
        if (v is T t)
            return t;

        if (v is null)
        {
            // допустим nullable T
            if (default(T) is null)
                return default!;
            throw new InvalidCastException($"Key '{key}' is null, cannot cast to {typeof(T).Name}.");
        }

        // Попытка универсального приведения (int→decimal, string→int и т.п.)
        try
        {
            return (T)System.Convert.ChangeType(v, typeof(T))!;
        }
        catch
        {
            throw new InvalidCastException($"Key '{key}' has type {v.GetType().Name}, expected {typeof(T).Name}.");
        }
    }

    /// <summary>Попробовать получить значение по ключу (типобезопасно).</summary>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_data.TryGetValue(key, out var v))
        {
            if (v is T t)
            {
                value = t;
                return true;
            }

            try
            {
                value = (T)System.Convert.ChangeType(v!, typeof(T))!;
                return true;
            }
            catch
            {
                /* ignore */
            }
        }

        value = default;
        return false;
    }

    /// <summary>Установить или обновить значение по ключу.</summary>
    public Bag Set(string key, object? value)
    {
        _data[key] = value;
        return this;
    }

    /// <summary>Проверить наличие ключа.</summary>
    public bool Has(string key) => _data.ContainsKey(key);

    /// <summary>Вернуть snapshot как словарь (копию).</summary>
    public IDictionary<string, object?> ToDictionary() => new Dictionary<string, object?>(_data, _data.Comparer);

    /// <summary>Итерируемое представление пар (ключ, значение).</summary>
    public IEnumerable<KeyValuePair<string, object?>> AsEnumerable() => _data;

    #region IReadOnlyDictionary реализация

    public int Count => _data.Count;

    public IEnumerable<string> Keys => _data.Keys;

    public IEnumerable<object?> Values => _data.Values;

    public bool ContainsKey(string key) => _data.ContainsKey(key);
    public bool TryGetValue(string key, out object? value) => _data.TryGetValue(key, out value);

    public object? this[string key] => _data[key];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _data.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}
