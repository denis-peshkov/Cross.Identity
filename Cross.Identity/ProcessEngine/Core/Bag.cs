namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Simple property bag for exchanging data between process steps.
/// Keys should be namespaced: "registration.Email", "auth.Phone", "user.Id", "auth.Token".
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

        // Attempt generic conversion (int→decimal, string→int, etc.)
        try
        {
            return (T)System.Convert.ChangeType(v, typeof(T))!;
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
