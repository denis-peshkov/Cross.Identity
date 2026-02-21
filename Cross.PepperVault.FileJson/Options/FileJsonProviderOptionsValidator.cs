namespace Cross.PepperVault.Options;

/// <summary>Проверки для профиля <c>Pepper.provider == "FileJson"</c>.</summary>
public sealed class FileJsonProviderOptionsValidator : AbstractValidator<PepperOptions<FileJsonProviderOptions>>
{
    public FileJsonProviderOptionsValidator()
    {
        Include(new PepperBaseValidator<FileJsonProviderOptions>());

        RuleFor(x => x.Provider)
            .Equal("FileJson", StringComparer.OrdinalIgnoreCase)
            .WithMessage("Pepper.provider must be 'FileJson'.");

        RuleFor(x => x.ProviderOptions.Path)
            .NotEmpty().WithMessage("providerOptions.path is required.")
            .Must(System.IO.Path.IsPathRooted).WithMessage("providerOptions.path must be an absolute path.");
    }
}
