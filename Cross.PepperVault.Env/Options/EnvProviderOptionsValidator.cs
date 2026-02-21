namespace Cross.PepperVault.Options;

public sealed class EnvProviderOptionsValidator : AbstractValidator<PepperOptions<EnvProviderOptions>>
{
    public EnvProviderOptionsValidator()
    {
        RuleFor(x => x.ProviderOptions.Current)
            .NotEmpty().WithMessage("providerOptions.current is required.");

        RuleFor(x => x.ProviderOptions.Peppers)
            .NotEmpty().WithMessage("providerOptions.prefix is required.");
    }
}
