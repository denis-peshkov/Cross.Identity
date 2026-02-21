namespace Cross.PepperVault.Options;

/// <summary>
/// Проверки для профиля <c>Pepper.provider == "AwsJson"</c>.
/// Ожидается один secret с JSON-содержимым: { "current": N, "peppers": { "1": "...", ... } }.
/// </summary>
public sealed class AwsSecretsJsonProviderOptionsValidator : AbstractValidator<PepperOptions<AwsSecretsJsonProviderOptions>>
{
    public AwsSecretsJsonProviderOptionsValidator()
    {
        RuleFor(x => x.Provider)
            .Equal("AwsJson", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'AwsJson'.");

        RuleFor(x => x.ProviderOptions.SecretId)
            .NotEmpty()
            .WithMessage("providerOptions.secretId is required.");
    }
}
