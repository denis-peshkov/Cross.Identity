namespace Cross.PepperVault.Options;

/// <summary>
/// Базовые проверки для всех вариантов <see cref="PepperOptions{TProviderOptions}"/>:
/// <list type="bullet">
/// <item><description><c>Pepper.provider</c> обязателен.</description></item>
/// <item><description><c>Pepper.ttl</c> &gt; 0.</description></item>
/// <item><description><c>Pepper.providerOptions</c> должен быть задан.</description></item>
/// </list>
/// </summary>
/// <typeparam name="TProviderOptions">Тип вложенных опций конкретного провайдера.</typeparam>
public sealed class PepperBaseValidator<TProviderOptions> : AbstractValidator<PepperOptions<TProviderOptions>>
{
    public PepperBaseValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("Pepper.provider is required.");

        RuleFor(x => x.Ttl)
            .GreaterThan(TimeSpan.Zero).WithMessage("Pepper.ttl must be > 0.");

        RuleFor(x => x.ProviderOptions)
            .NotNull().WithMessage("Pepper.providerOptions must be present.");
    }
}
