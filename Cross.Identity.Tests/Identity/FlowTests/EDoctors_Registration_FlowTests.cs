namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class EDoctors_Registration_FlowTests : RunFlowCommandHandlerTestsBase
{
    private string FLOW => "edoctors";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent"
        };

        // Register step factories
        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<CreateUserStepFactory>();
        AddRegistryStep<SendCodeStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        // Configure service provider to return requested services
        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(
            headersContextAccessor);
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(
            _processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService(headersContextAccessor));
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
                Mock.Of<ISmsSenderService>(),
                configuration));
    }

    [Test]
    public async Task Handle_LicenseRegistration_SuccessfulExecution()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["FirstName"] = "John",
            ["LastName"] = "Smith",
            ["Password"] = "P@ssw0rd!",
            ["ConfirmPassword"] = "P@ssw0rd!",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("LastCode");
        payload["LastCode"].Should().BeOfType<string>().Which.Should().HaveLength(8);
        // verify GetService<T>() calls
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ICodeService)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IHostEnvironment)), Times.Once);
        // (optional) verify total number of invocations
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(9);
    }

    [Test]
    public async Task Handle_InvalidInput_ShouldThrowValidationException()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["FirstName"] = "J", // name too short
            ["LastName"] = "C", // company name too short
            ["Password"] = "123", // password too short
            ["ConfirmPassword"] = "456", // does not match
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // verify that an error message is present
    }

    [Test]
    public async Task CollectForm_Should_Validate_Passwords_Equal()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["FullName"] = "John Tester",
            ["Company"] = "Company Inc",
            ["Password"] = "P@ssw0rd!",
            ["ConfirmPassword"] = "P@ssw0rd!--", // does not match
            ["AcceptGetEmails"] = true,
            ["AcceptLicenseTerms"] = true,
        };

        // Act & Assert
        await FluentActions.Invoking(() => _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*Passwords do not match*");
    }
}
