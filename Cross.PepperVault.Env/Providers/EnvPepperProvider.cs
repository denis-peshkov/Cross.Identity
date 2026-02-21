namespace Cross.PepperVault.Providers;

public sealed class EnvPepperProvider : PepperProviderBase
{
    private readonly PepperOptions<EnvProviderOptions> _options;

    public EnvPepperProvider(
        IOptionsMonitor<PepperOptions<EnvProviderOptions>> monitor)
        : base(new OptionsMonitorProxyForTtl<EnvProviderOptions>(monitor))
    {
        _options = monitor.CurrentValue;
    }

    /// <inheritdoc />
    protected override Task<(short current, IReadOnlyDictionary<short, string> peppers)> LoadAsync(CancellationToken cancellationToken)
    {
        var opts = _options.ProviderOptions
                   ?? throw new InvalidOperationException("Pepper: ProviderOptions are not configured.");

        // Валидируем
        if (opts.Peppers is null || opts.Peppers.Count == 0)
            throw new InvalidOperationException("Pepper: 'Peppers' must be non-empty.");

        if (!opts.Peppers.TryGetValue(opts.Current, out var currentPepper) ||
            string.IsNullOrWhiteSpace(currentPepper))
            throw new InvalidOperationException(
                $"Pepper: current version '{opts.Current}' not found in Peppers or is empty.");

        // Можно добавить дополнительные проверки: уникальность значений, минимальная длина и т.п.
        var ro = new ReadOnlyDictionary<short, string>(opts.Peppers);
        return Task.FromResult<(short, IReadOnlyDictionary<short, string>)>((opts.Current, ro));

    }
}
