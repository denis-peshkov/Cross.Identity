namespace Cross.Identity.UnitTests.Core;

public class RunFlowCommandHandlerTestsBase : EFTestsBase
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

    public void Initialize()
    {
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        // Регистрируем все фабрики шагов, как в DI-расширении AddCrossIdentity
        _registry = new StepRegistry();
        _registry.Register(new CodeAuthStepFactory());
        _registry.Register(new CollectFormStepFactory());
        _registry.Register(new CollectResultStepFactory());
        _registry.Register(new CreateUserStepFactory());
        _registry.Register(new ForgotPasswordStepFactory());
        _registry.Register(new GetUserStepFactory());
        _registry.Register(new PasswordAuthStepFactory());
        _registry.Register(new RefreshTokenStepFactory());
        _registry.Register(new ResetPasswordStepFactory());
        _registry.Register(new SendCodeStepFactory());
        _registry.Register(new TokenStepFactory());
        _registry.Register(new VerifyCodeStepFactory());
        var formValidatorFactory = new UnifiedFormValidatorFactory();
        _requestInput = new RequestInput();

        // Настраиваем правильную цепочку зависимостей
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
            .Returns(new LoggerFactory());
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IConfiguration)))
            .Returns(new ConfigurationBuilder().Build());
        var env = new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "UnitTests",
            ContentRootPath = AppContext.BaseDirectory
        };
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(IHostEnvironment)))
            .Returns(env);
    }

    public void RegisterToServiceProvider<I, T>(T instance)
        where T :  class
    {
        _serviceProviderMock
            .Setup(x => x.GetService(typeof(I)))
            .Returns(instance);
    }

    /// <summary>
    /// Register step factories
    /// </summary>
    /// <typeparam name="T">See <inheritdoc cref="IStepFactory"/>.</typeparam>
    public void AddRegistryStep<T>()
        where T : IStepFactory, new()
    {
        var factory = new T();              // явное создание экземпляра
        _registry.Register(factory);        // регистрация в реестре
    }

    [Obsolete]
    public void AddJson(string json)
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
        (_processDefinitionProvider as IDisposable)?.Dispose();

        base.TearDown();
    }
}
