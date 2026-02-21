namespace Cross.PepperVault.Options;

/// <summary>
/// Проверки для профиля <c>Pepper.provider == "HcvKv2Json"</c>.
/// Ожидается, что KV v2 вернёт JSON-объект с полями <c>current</c> и <c>peppers</c>.
/// </summary>
public sealed class HcvKv2JsonProviderOptionsValidator : AbstractValidator<PepperOptions<HcvKv2JsonProviderOptions>>
{
    public HcvKv2JsonProviderOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .Equal("HcvKv2Json", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'HcvKv2Json'.");

        RuleFor(x => x.ProviderOptions.VaultUri)
            .NotEmpty().WithMessage("providerOptions.vaultUri is required.")
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https")
            .WithMessage("providerOptions.vaultUri must be absolute http/https URI.");

        RuleFor(x => x.ProviderOptions.Token)
            .NotEmpty().WithMessage("providerOptions.token is required.");

        RuleFor(x => x.ProviderOptions.Mount)
            .NotEmpty().WithMessage("providerOptions.mount is required.");

        RuleFor(x => x.ProviderOptions.Path)
            .NotEmpty().WithMessage("providerOptions.path is required.");
    }
}
