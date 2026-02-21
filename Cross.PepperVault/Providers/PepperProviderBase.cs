namespace Cross.PepperVault.Providers;

public abstract class PepperProviderBase : IPepperVaultProvider
{
    private readonly TimeSpan _ttl;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private volatile bool _initialized;
    private DateTime _loadedAtUtc;

    private short _current;
    private IReadOnlyDictionary<short, string> _peppers = new ReadOnlyDictionary<short, string>(new Dictionary<short, string>());

    protected PepperProviderBase(IOptionsMonitor<TimeSpan> ttlMonitor)
    {
        _ttl = ttlMonitor.CurrentValue;
    }

    public short CurrentVersion
    {
        get
        {
            EnsureFreshAsync();
            return _current;
        }
    }

    public IReadOnlyDictionary<short, string> Peppers
    {
        get
        {
            EnsureFreshAsync();
            return _peppers;
        }
    }

    public bool TryGetCurrentVersion([MaybeNullWhen(false)] out string pepper)
    {
        EnsureFreshAsync();
        return _peppers.TryGetValue(_current, out pepper);
    }

    public bool TryGet(short version, [MaybeNullWhen(false)] out string pepper)
    {
        EnsureFreshAsync();
        return _peppers.TryGetValue(version, out pepper);
    }

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        // защита от «табунного набега»
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var (current, peppers) = await LoadAsync(cancellationToken).ConfigureAwait(false);
            if (!peppers.ContainsKey(current))
                throw new InvalidOperationException("Current pepper missing in provider data.");

            Interlocked.Exchange(ref _peppers, peppers);
            Interlocked.Exchange(ref Unsafe.As<short, int>(ref _current), current);
            _loadedAtUtc = DateTime.UtcNow;
            _initialized = true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// Реальная загрузка из источника: вернуть (current, map).
    protected abstract Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken);

    private void EnsureFreshAsync()
    {
        if (!_initialized || DateTime.UtcNow - _loadedAtUtc > _ttl)
        {
            ReloadAsync().GetAwaiter().GetResult();
        }
    }
}
