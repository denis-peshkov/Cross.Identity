namespace Cross.PepperVault.Options;

/// <summary>
/// Проверки для профиля <c>Pepper.provider == "AzureKvJson"</c>.
/// Требуется JSON-секрет в Azure Key Vault со схемой:
/// <code>
/// {
///   "current": 3,
///   "peppers": { "1": "...", "2": "...", "3": "..." }
/// }
/// </code>
/// </summary>
public sealed class AzureKvJsonProviderOptionsValidator : AbstractValidator<PepperOptions<AzureKvJsonProviderOptions>>
{
    public AzureKvJsonProviderOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .Equal("AzureKvJson", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'AzureKvJson'.");

        RuleFor(x => x.ProviderOptions.VaultUri)
            .NotNull()
            .WithMessage("providerOptions.vaultUri is required.")
            .Must(u => u is { IsAbsoluteUri: true } && u.Scheme is "https")
            .WithMessage("providerOptions.vaultUri must be absolute https URI.");

        RuleFor(x => x.ProviderOptions.SecretName)
            .NotEmpty()
            .WithMessage("providerOptions.secretName is required.");
    }
}
