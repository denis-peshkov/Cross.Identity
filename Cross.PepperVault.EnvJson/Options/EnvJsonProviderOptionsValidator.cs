namespace Cross.PepperVault.Options;

public sealed class EnvJsonProviderOptionsValidator : AbstractValidator<PepperOptions<EnvJsonProviderOptions>>
{
    public EnvJsonProviderOptionsValidator()
    {
        RuleFor(x => x.ProviderOptions.JsonKey)
            .NotEmpty().WithMessage("providerOptions.jsonKey is required.");
    }
}
