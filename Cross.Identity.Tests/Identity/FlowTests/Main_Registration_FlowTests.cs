namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_Registration_FlowTests : RunFlowCommandHandlerTestsBase
{
    private string FLOW => "main";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        // Register step factories
        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<CreateUserStepFactory>();
        AddRegistryStep<SendCodeStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        // Configure service provider to return requested services
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(
            _processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(), configuration, TestAuthOptions.Snapshot()));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidRegistrationInput_WhenExecuteRegisterFlow_ThenSucceedsAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Password"] = "P@ssw0rd!",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("UserId");
        payload["UserId"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        // verify GetService<T>() calls
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ICodeService)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IHostEnvironment)), Times.Once);
        // (optional) verify total number of invocations
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(10);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidRegistrationInput_WhenExecuteRegisterFlow_ThenThrowsValidationExceptionAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["FullName"] = "J", // name too short
            ["Company"] = "C", // company name too short
            ["Password"] = "123", // password too short
            ["AcceptLicenseTerms"] = false // required field
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // verify that an error message is present
    }
}
