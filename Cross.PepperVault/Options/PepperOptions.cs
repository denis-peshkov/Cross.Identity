namespace Cross.PepperVault.Options;

public sealed record PepperOptions<TProviderOptions>
{
    public required string Provider { get; init; }

    public TimeSpan Ttl { get; init; } = TimeSpan.FromMinutes(10);

    public required TProviderOptions ProviderOptions { get; init; }
}
