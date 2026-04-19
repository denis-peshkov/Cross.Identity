namespace Cross.Identity.Extensions;

/// <summary>
/// DI-расширения для регистрации провайдера JSON-дефиниций из embedded-ресурсов.
/// </summary>
public static class ServiceCollectionExtensions
{
      private static IServiceCollection AddJwtTokenAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionName));
        services.TryAddScoped<IJwtTokenService, JwtTokenService>();

        // var authOptions = new AuthenticationOptions();
        // configuration.GetSection("Authentication").Bind(authOptions);

        return services;
    }

    public static IServiceCollection AddCrossIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddJwtTokenAuth(configuration)
            .AddFlowDefinitionsCompositeFromDirectoryAndEmbedded(configuration);

        services.TryAddScoped<IRequestInput, RequestInput>();

        services.TryAddScoped<IUserService, UserService>();
        services.TryAddSingleton<IPasswordHasher, PasswordHasher>();
        services.TryAddSingleton<IPhoneNormalizer, PhoneNormalizer>();
        services.Configure<Cross.Identity.Services.Crypto.PasswordHasherOptions>(configuration.GetSection("PasswordHasher"));
        // services.AddPepperOptions<EnvProviderOptions, EnvProviderOptionsValidator>(configuration);

        services.TryAddScoped<ICodeService, CodeService>();
        services.TryAddScoped<IEmailSenderService, EmailSenderService>();
        services.TryAddScoped<ISmsSenderService, SmsSenderService>();

        // using var provider = services.BuildServiceProvider(validateScopes: true);
        // provider.GetRequiredService<ICodeService>();

        services.TryAddScoped<IFlowExecutor, FlowExecutor>();
        services.TryAddScoped<StepRegistry>();
        // регистрируем ВСЕ реализации IStepFactory
        services.TryAddEnumerable(
            new[]
            {
                ServiceDescriptor.Scoped<IStepFactory, CodeAuthStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, CollectFormStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, CollectResultStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, CreateUserStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, ForgotPasswordStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, GetUserIdStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, PasswordAuthStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, RefreshTokenStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, ResetPasswordStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, SendCodeStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, TokenStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, VerifyCodeStepFactory>(),
            });

        services.TryAddScoped<IFormValidatorFactory, UnifiedFormValidatorFactory>();

        var rsaKey = JwtKeys.GetRsaKey();
        services.AddSingleton<RsaSecurityKey>(rsaKey);

        return services;
    }

    /// <summary>
    /// Добавляет клайм только если значение не пустое (null/пустая строка игнорируются).
    /// </summary>
    public static List<Claim> AddIfNotNull(this List<Claim> claims, string claimType, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            claims.Add(new Claim(claimType, value));

        return claims;
    }

    /// <summary>
    /// Композит: сначала файловая система, затем embedded-ресурсы библиотеки/приложения.
    /// </summary>
    public static IServiceCollection AddFlowDefinitionsCompositeFromDirectoryAndEmbedded(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileSystemProcessDefinitionOptions>(configuration.GetSection("FileSystemProcessDefinition"));
        services.Configure<EmbeddedProcessDefinitionOptions>(configuration.GetSection("EmbeddedProcessDefinition"));

        var descriptors = new []
        {
            ServiceDescriptor.Singleton<IProcessDefinitionProvider, FileSystemProcessDefinitionProvider>(),
            ServiceDescriptor.Singleton<IProcessDefinitionProvider, EmbeddedResourceProcessDefinitionProvider>(),
        };

        services.AddFlowDefinitionsComposite(descriptors);

        return services;
    }

    /// <summary>
    /// Зарегистрировать композит из произвольного набора провайдеров (в указанном порядке).
    /// </summary>
    public static IServiceCollection AddFlowDefinitionsComposite(this IServiceCollection services, params ServiceDescriptor[] descriptors)
    {
        if (descriptors is null || descriptors.Length == 0)
            throw new ArgumentException("At least one provider required.", nameof(descriptors));

        // регистрируем ВСЕ реализации IProcessDefinitionProvider
        services.TryAddEnumerable(descriptors);

        services.AddSingleton<CompositeProcessDefinitionProvider>();

        return services;
    }
}
