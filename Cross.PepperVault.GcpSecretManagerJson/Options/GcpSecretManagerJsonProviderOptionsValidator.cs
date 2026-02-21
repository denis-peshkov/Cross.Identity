namespace Cross.PepperVault.Options;

/// <summary>
/// Проверки для профиля <c>Pepper.provider == "GcpJson"</c>.
/// Ожидается один secret со схемой JSON: { current, peppers }.
/// </summary>
public sealed class GcpSecretManagerJsonProviderOptionsValidator : AbstractValidator<PepperOptions<GcpSecretManagerJsonProviderOptions>>
{
    public GcpSecretManagerJsonProviderOptionsValidator()
    {
        // Общие инварианты (Provider, Ttl, ProviderOptions != null)
        Include(new PepperBaseValidator<GcpSecretManagerJsonProviderOptions>());

        RuleFor(x => x.Provider)
            .Equal("GcpJson", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'GcpJson'.");

        RuleFor(x => x.ProviderOptions.SecretName)
            .NotEmpty().WithMessage("providerOptions.secretName is required.")
            .Must(s => SecretName.TryParse(s, out _))
            .WithMessage("providerOptions.secretName must match 'projects/{projectId}/secrets/{secretId}'.");
    }
}
