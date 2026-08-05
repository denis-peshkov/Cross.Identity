namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class Main_ForgotPassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string Email = "test@example.com";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<ForgotPasswordStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(headersContextAccessor);
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(
            new UserService(
                Context,
                Mock.Of<ILogger<UserService>>(),
                Mock.Of<IPepperVaultProvider>(),
                Mock.Of<IPasswordHasher>(),
                Mock.Of<IPhoneNormalizer>(),
                headersContextAccessor,
                Mock.Of<IJwtTokenService>()));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true",
            })
            .Build();
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                configuration));

        RegisterToServiceProvider<IHostEnvironment, IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "UnitTests",
            ContentRootPath = AppContext.BaseDirectory,
        });

        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = Email,
        });
    }

    [Test]
    public async Task ForgotPassword_WithValidEmail_ShouldReturnLastCode()
    {
        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = Email },
            Flow,
            FlowOperationEnum.ForgotPassword,
            CancellationToken.None);

        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("LastCode");
        payload["LastCode"].Should().BeOfType<string>().Which.Should().HaveLength(8);
    }

    [Test]
    public async Task ForgotPassword_InProductionMode_ShouldOmitLastCode()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "false",
            })
            .Build();
        RegisterToServiceProvider<IConfiguration, IConfiguration>(configuration);
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                configuration));

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = Email },
            Flow,
            FlowOperationEnum.ForgotPassword,
            CancellationToken.None);

        if (result.Data is Dictionary<string, object?> payload)
        {
            payload.Should().NotContainKey("LastCode");
        }
        else
        {
            result.Data.Should().BeNull();
        }
    }

    [Test]
    public async Task ForgotPassword_WithInvalidEmail_ShouldThrowValidationException()
    {
        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = "invalid-email" },
            Flow,
            FlowOperationEnum.ForgotPassword,
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
