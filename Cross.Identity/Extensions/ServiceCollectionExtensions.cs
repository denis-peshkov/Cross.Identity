namespace Cross.Identity.Extensions;

/// <summary>
/// DI extensions for registering Cross.Identity infrastructure:
/// identity services, process engine, steps, validators, and flow-definition providers.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static IServiceCollection AddJwtTokenAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthenticationOptions>(configuration.GetSection(AuthenticationOptions.SectionName));
        services.TryAddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHostedService<ExpiredRefreshTokenCleanupHostedService>();

        // var authOptions = new AuthenticationOptions();
        // configuration.GetSection("Authentication").Bind(authOptions);

        return services;
    }

    /// <summary>
    /// Registers core Cross.Identity services and dependencies in the DI container.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The current service collection for fluent chaining.</returns>
    public static IServiceCollection AddCrossIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var identityConfiguration = new IdentityServiceConfiguration();
        configuration.GetSection(IdentityServiceConfiguration.SectionName).Bind(identityConfiguration);
        services.AddSingleton(identityConfiguration);
        services.AddSingleton<LicenseAccessor>();
        services.AddSingleton<LicenseValidator>();
        services.AddSingleton<ILicenseProductInfo, LicenseProductInfo>();

        services
            .AddJwtTokenAuth(configuration)
            .AddExternalLogin(configuration)
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
        // register ALL IStepFactory implementations
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
                ServiceDescriptor.Scoped<IStepFactory, ExternalLoginInitiateStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, ExternalLoginCompleteStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, ExternalLoginUnlinkStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, LogoutStepFactory>(),
                ServiceDescriptor.Scoped<IStepFactory, LogoutAllStepFactory>(),
            });

        services.TryAddScoped<IFormValidatorFactory, UnifiedFormValidatorFactory>();

        var rsaKey = JwtKeys.GetRsaKey();
        services.AddSingleton<RsaSecurityKey>(rsaKey);

        return services;
    }

    private static IServiceCollection AddExternalLogin(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ExternalLoginOptions>(configuration.GetSection(ExternalLoginOptions.SectionName));
        services.AddHttpClient(nameof(ExternalLoginService));
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.TryAddScoped<IExternalLoginService, ExternalLoginService>();

        return services;
    }

    /// <summary>
    /// Registers a composite flow-definition provider:
    /// filesystem first, then fallback to embedded resources.
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
    /// Register a composite from an arbitrary set of providers (in the given order).
    /// </summary>
    public static IServiceCollection AddFlowDefinitionsComposite(this IServiceCollection services, params ServiceDescriptor[] descriptors)
    {
        if (descriptors is null || descriptors.Length == 0)
            throw new ArgumentException("At least one provider required.", nameof(descriptors));

        // register ALL IProcessDefinitionProvider implementations
        services.TryAddEnumerable(descriptors);

        services.AddSingleton<CompositeProcessDefinitionProvider>();

        return services;
    }
}
