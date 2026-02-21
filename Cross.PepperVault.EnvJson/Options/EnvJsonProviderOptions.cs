namespace Cross.PepperVault.Options;

public sealed record EnvJsonProviderOptions
{
    public string JsonKey { get; init; } = "AUTH_PEPPERS_JSON";
}
