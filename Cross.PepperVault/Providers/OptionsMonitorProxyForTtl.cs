namespace Cross.PepperVault.Providers;

public sealed class OptionsMonitorProxyForTtl<TProviderOptions> : IOptionsMonitor<TimeSpan>
{
    private readonly IOptionsMonitor<PepperOptions<TProviderOptions>> _inner;
    public OptionsMonitorProxyForTtl(IOptionsMonitor<PepperOptions<TProviderOptions>> inner) => _inner = inner;

    public TimeSpan CurrentValue => _inner.CurrentValue.Ttl;
    public TimeSpan Get(string? name) => _inner.Get(name).Ttl;
    public IDisposable OnChange(Action<TimeSpan, string?> listener)
        => _inner.OnChange((o, n) => listener(o.Ttl, n));
}
