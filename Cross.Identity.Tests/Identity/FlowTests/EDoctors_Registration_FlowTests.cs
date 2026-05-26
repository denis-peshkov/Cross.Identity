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
        RegisterToServiceProvider<IUserService, IUserService>(
            new UserService(
                Context,
                Mock.Of<ILogger<UserService>>(),
                Mock.Of<IPepperVaultProvider>(),
                Mock.Of<IPasswordHasher>(),
                Mock.Of<IPhoneNormalizer>(),
                headersContextAccessor));
        var notificationOptions = new Mock<IOptionsSnapshot<MessagingEmailOptions>>();
        notificationOptions.Setup(o => o.Value).Returns(new MessagingEmailOptions());
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                notificationOptions.Object));
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
        // проверка вызовов GetService<T>()
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ICodeService)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IHostEnvironment)), Times.Once);
        // (необязательно) проверить суммарное число обращений
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(9);
    }

    [Test]
    public async Task Handle_InvalidInput_ShouldThrowValidationException()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["FirstName"] = "J", // слишком короткое имя
            ["LastName"] = "C", // слишком короткое название
            ["Password"] = "123", // слишком короткий пароль
            ["ConfirmPassword"] = "456", // не совпадает
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // проверяем что есть сообщение об ошибке
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
            ["ConfirmPassword"] = "P@ssw0rd!--", // не совпадает
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
