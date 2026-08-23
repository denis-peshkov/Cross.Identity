namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Simple property bag for exchanging data between process steps.
/// Keys should be namespaced: "registration.Email", "auth.PhoneNumber", "user.Id", "auth.Token".
/// </summary>
public sealed class Bag : IReadOnlyDictionary<string, object?>
{
    private readonly Dictionary<string, object?> _data = new(StringComparer.Ordinal);

    /// <summary>Get a value by key with type conversion.</summary>
    public T Get<T>(string key)
    {
        if (!_data.TryGetValue(key, out var v))
            throw new KeyNotFoundException($"Key '{key}' not found.");

        // exact cast, or Convert.ChangeType for primitives
        if (v is T t)
            return t;

        if (v is null)
        {
            // allow nullable T
            if (default(T) is null)
                return default!;
            throw new InvalidCastException($"Key '{key}' is null, cannot cast to {typeof(T).Name}.");
        }

        // Guid is not handled by Convert.ChangeType from string (form fields are strings).
        if (v is string text)
        {
            if (typeof(T) == typeof(Guid))
            {
                if (Guid.TryParse(text, out var guid))
                    return (T)(object)guid;
            }
            else if (Nullable.GetUnderlyingType(typeof(T)) == typeof(Guid))
            {
                // Optional Guid?: empty / invalid → null (e.g. optional link UserId).
                if (string.IsNullOrWhiteSpace(text) || !Guid.TryParse(text, out var optionalGuid))
                    return default!;
                return (T)(object)optionalGuid;
            }
        }

        // Attempt generic conversion (int→decimal, string→int, etc.)
        try
        {
            return (T)Convert.ChangeType(v, GetConversionType<T>())!;
        }
        catch
        {
            throw new InvalidCastException($"Key '{key}' has type {v.GetType().Name}, expected {typeof(T).Name}.");
        }
    }

    /// <summary>Try to get a value by key (type-safe).</summary>
    public bool TryGet<T>(string key, out T? value)
    {
        if (_data.TryGetValue(key, out var v))
        {
            if (v is T t)
            {
                value = t;
                return true;
            }

            if (v is null)
            {
                if (default(T) is null)
                {
                    value = default;
                    return true;
                }

                value = default;
                return false;
            }

            if (v is string text)
            {
                if (typeof(T) == typeof(Guid))
                {
                    if (Guid.TryParse(text, out var guid))
                    {
                        value = (T)(object)guid;
                        return true;
                    }

                    value = default;
                    return false;
                }

                if (Nullable.GetUnderlyingType(typeof(T)) == typeof(Guid))
                {
                    if (string.IsNullOrWhiteSpace(text) || !Guid.TryParse(text, out var optionalGuid))
                    {
                        value = default;
                        return true;
                    }

                    value = (T)(object)optionalGuid;
                    return true;
                }
            }

            try
            {
                value = (T)Convert.ChangeType(v, GetConversionType<T>())!;
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

    private static Type GetConversionType<T>() => Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

    /// <summary>Set or update a value by key.</summary>
    public Bag Set(string key, object? value)
    {
        _data[key] = value;
        return this;
    }

    /// <summary>Check whether a key exists.</summary>
    public bool Has(string key) => _data.ContainsKey(key);

    /// <summary>Return a snapshot as a dictionary (copy).</summary>
    public IDictionary<string, object?> ToDictionary() => new Dictionary<string, object?>(_data, _data.Comparer);

    /// <summary>Enumerable view of (key, value) pairs.</summary>
    public IEnumerable<KeyValuePair<string, object?>> AsEnumerable() => _data;

    #region IReadOnlyDictionary implementation

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
