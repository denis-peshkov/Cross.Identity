namespace Cross.PepperVault.Options;

/// <summary>
/// Проверки для профиля <c>Pepper.provider == "AzureKvVersions"</c>.
/// Ожидается, что версии секрета помечены тегом с именем <c>TagName</c> (по умолчанию <c>pepperVersion</c>).
/// </summary>
public sealed class AzureKvVersionsProviderOptionsValidator : AbstractValidator<PepperOptions<AzureKvVersionsProviderOptions>>
{
    public AzureKvVersionsProviderOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .Equal("AzureKvVersions", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'AzureKvVersions'.");

        RuleFor(x => x.ProviderOptions.VaultUri)
            .NotNull().WithMessage("providerOptions.vaultUri is required.")
            .Must(u => u is { IsAbsoluteUri: true } && u.Scheme is "https")
            .WithMessage("providerOptions.vaultUri must be absolute https URI.");

        RuleFor(x => x.ProviderOptions.SecretName)
            .NotEmpty().WithMessage("providerOptions.secretName is required.");

        RuleFor(x => x.ProviderOptions.TagName)
            .NotEmpty().WithMessage("providerOptions.tagName is required.");
    }
}
