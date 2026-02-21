namespace Cross.PepperVault.Extensions;

/// <summary>
/// Расширения для регистрации опций и валидаторов Pepper.
/// </summary>
public static class PepperVaultExtensions
{
    /// <summary>
    /// Регистрирует опции <see cref="PepperOptions{TProviderOptions}"/> из секции <c>Pepper</c>,
    /// подключает базовые и провайдерные FluentValidation-валидаторы через композитный адаптер
    /// и включает проверку на старте (<c>ValidateOnStart()</c>).
    /// </summary>
    public static IServiceCollection AddPepperOptions<TProviderOptions, TProviderOptionsValidator>(this IServiceCollection services, IConfiguration cfg)
        where TProviderOptionsValidator : class, IValidator<PepperOptions<TProviderOptions>>
    {
        // Два независимых валидатора для одного типа (база + провайдерный)
        services.AddSingleton<IValidator<PepperOptions<TProviderOptions>>, PepperBaseValidator<TProviderOptions>>();
        services.AddSingleton<IValidator<PepperOptions<TProviderOptions>>, TProviderOptionsValidator>();

        // Регистрируем единый композитный адаптер прогонит все зарегистрированные IValidator<T> для типа
        services.AddSingleton<IValidateOptions<PepperOptions<TProviderOptions>>,
            CompositeFluentOptionsValidatorAdapter<PepperOptions<TProviderOptions>>>();

        // Биндим и включаем валидацию при старте
        services
            .AddOptions<PepperOptions<TProviderOptions>>()
            .Bind(cfg.GetSection("Pepper"))
            .ValidateOnStart(); // дернёт IValidateOptions

        return services;
    }
}
