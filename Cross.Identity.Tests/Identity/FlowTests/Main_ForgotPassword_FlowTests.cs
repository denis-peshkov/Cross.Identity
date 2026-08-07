namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_ForgotPassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string Email = "test@example.com";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<ForgotPasswordStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(
            CreateUserService());

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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailInDevelopmentMode_WhenForgotPasswordFlow_ThenReturnsLastCodeAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailInProductionMode_WhenForgotPasswordFlow_ThenOmitsLastCodeAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidEmail_WhenForgotPasswordFlow_ThenThrowsValidationExceptionAsync()
    {
        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = "invalid-email" },
            Flow,
            FlowOperationEnum.ForgotPassword,
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }
}
