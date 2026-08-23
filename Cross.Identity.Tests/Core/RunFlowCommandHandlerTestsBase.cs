namespace Cross.Identity.Tests.Core;

/// <summary>
/// End-to-end process engine tests: DI, step registry, flow executor, EF.
/// </summary>
internal class RunFlowCommandHandlerTestsBase : EFTestsBase
{
    // ReSharper disable InconsistentNaming
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected Mock<IServiceProvider> _serviceProviderMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private StepRegistry _registry;
    protected IProcessDefinitionProvider _processDefinitionProvider;
    protected IFlowExecutor _flowExecutor;
    protected IRequestInput _requestInput;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    // ReSharper restore InconsistentNaming

    protected void Initialize()
    {
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        // Register all step factories, as in the AddCrossIdentity DI extension
        _registry = new StepRegistry();
        _registry.Register(new CollectFormStepFactory());
        _registry.Register(new CollectResultStepFactory());
        _registry.Register(new CreateUserStepFactory());
        _registry.Register(new GetUserAccountIdStepFactory());
        _registry.Register(new PasswordAuthStepFactory());
        _registry.Register(new RefreshTokenStepFactory());
        _registry.Register(new ResetPasswordStepFactory());
        _registry.Register(new SendCodeStepFactory());
        _registry.Register(new TokenStepFactory());
        _registry.Register(new VerifyCodeStepFactory());
        _registry.Register(new ExternalLoginInitiateStepFactory());
        _registry.Register(new ExternalLoginCompleteStepFactory());
        _registry.Register(new ExternalLoginUnlinkStepFactory());
        _registry.Register(new ExternalLoginGetAllStepFactory());
        _registry.Register(new LogoutStepFactory());
        _registry.Register(new LogoutAllStepFactory());
        _registry.Register(new VerifyTokenStepFactory());
        _registry.Register(new CommunicationEndpointsGetAllStepFactory());
        _registry.Register(new CommunicationEndpointSetPreferredStepFactory());
        var formValidatorFactory = new UnifiedFormValidatorFactory();
        _requestInput = new RequestInput();
        var identityConfiguration = new IdentityServiceConfiguration();
        var loggerFactory = new LoggerFactory();
        var licenseAccessor = new LicenseAccessor(identityConfiguration, loggerFactory);
        var licenseValidator = new LicenseValidator(loggerFactory);
        var licenseProductInfo = new LicenseProductInfo();

        // Configure the correct dependency chain
        _serviceScopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);
        _serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(_serviceProviderMock.Object);

        // Configure EmbeddedResourceProcessDefinitionProvider through options to match production code
        var embeddedOptions = Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions"
        });

        _processDefinitionProvider = new EmbeddedResourceProcessDefinitionProvider(embeddedOptions);

        _flowExecutor = new FlowExecutor(
            _serviceProviderMock.Object,
            _registry,
            _processDefinitionProvider,
            _requestInput);

        // Configure service provider to return requested services
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IFormValidatorFactory)))
            .Returns(formValidatorFactory);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IRequestInput)))
            .Returns(_requestInput);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(ILoggerFactory)))
            .Returns(loggerFactory);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(LicenseAccessor)))
            .Returns(licenseAccessor);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(LicenseValidator)))
            .Returns(licenseValidator);
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IEnumerable<ILicenseProductInfo>)))
            .Returns(new ILicenseProductInfo[] { licenseProductInfo });
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IConfiguration)))
            .Returns(new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Authentication:ClientUrl"] = "http://localhost:4200",
                    ["Authentication:DeveloperMode"] = "true"
                })
                .Build());
        var env = new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "UnitTests",
            ContentRootPath = AppContext.BaseDirectory
        };
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IHostEnvironment)))
            .Returns(env);

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock
            .Setup(j => j.EnsureRefreshTokenBelongsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(jwtMock.Object);

        RegisterToServiceProvider<ICommunicationEndpointService, ICommunicationEndpointService>(
            new CommunicationEndpointService(Context, new AuditService(Context), jwtMock.Object, TestAuthOptions.Snapshot()));
    }

    protected void RegisterToServiceProvider<I, T>(T instance)
        where T :  class
    {
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(I)))
            .Returns(instance);
    }

    protected UserService CreateUserService()
    {
        var pepperVault = new Mock<IPepperVaultProvider>();
        pepperVault.Setup(p => p.CurrentVersion).Returns((short)1);
        string? pepperValue = "test-pepper";
        pepperVault
            .Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny))
            .Returns((out string v) =>
            {
                v = pepperValue!;
                return true;
            });

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("$pbkdf2-test-hash");

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock
            .Setup(j => j.EnsureRefreshTokenBelongsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        jwtMock
            .Setup(j => j.RevokeAllTokensForUserAsync(
                It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<HostSuppliedClientContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var communicationEndpoints = new CommunicationEndpointService(
            Context,
            new AuditService(Context),
            jwtMock.Object,
            TestAuthOptions.Snapshot());

        return new UserService(
            Context,
            Mock.Of<ILogger<UserService>>(),
            pepperVault.Object,
            passwordHasher.Object,
            jwtMock.Object,
            communicationEndpoints,
            communicationEndpoints,
            CreateUserServiceOptions());
    }

    private static IOptionsSnapshot<AuthenticationOptions> CreateUserServiceOptions()
    {
        var mock = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        mock.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Lockout = new AuthenticationOptions.LockoutOptions(),
        });
        return mock.Object;
    }

    /// <summary>
    /// Register step factories
    /// </summary>
    /// <typeparam name="T">See <inheritdoc cref="IStepFactory"/>.</typeparam>
    protected void AddRegistryStep<T>()
        where T : IStepFactory, new()
    {
        var factory = new T();              // explicit instance creation
        _registry.Register(factory);        // register in the registry
    }

    [Obsolete]
    protected void AddJson(string json)
    {
        // _definitionProviderMock
        //     .Setup(x => x.GetJson(It.IsAny<string>(), It.IsAny<FlowOperationEnum>()))
        //     .Returns(json);
        //
        // _handler = new LicenseRegistration(
        //     _serviceProviderMock.Object,
        //     _registry,
        //     _definitionProviderMock.Object);
    }

    [TearDown]
    public override void TearDown()
    {
        LicenseCheckExtensions.ResetLicenseCheckForTests();
        (_processDefinitionProvider as IDisposable)?.Dispose();

        base.TearDown();
    }
}
