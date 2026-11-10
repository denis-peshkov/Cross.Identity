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
        payload.Should().ContainKey("UserAccountId");
        payload["UserAccountId"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
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
    public async Task GivenEmailWithoutPassword_WhenExecuteRegisterFlow_ThenSucceeds()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "nopass@example.com",
        };

        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        result.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("UserAccountId");
        payload["UserAccountId"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();

        var userAccountId = Guid.Parse((string)payload["UserAccountId"]!);
        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.PasswordPhc.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPhoneNumberOnly_WhenExecuteRegisterFlow_ThenSucceedsAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["PhoneNumber"] = "+12125551234",
            ["Password"] = "P@ssw0rd!",
        };

        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var userAccountId = Guid.Parse((string)payload["UserAccountId"]!);
        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.PhoneNumber.Should().Be("+12125551234");
        user.Email.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserNameOnly_WhenExecuteRegisterFlow_ThenThrowsValidationExceptionAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["UserName"] = "onlyuser",
            ["Password"] = "P@ssw0rd!",
        };

        // UserName is an optional alias; stock Register requires Email or Phone for OTP.
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*email or a phone*");

        (await Context.UsersAccounts.CountAsync(u => u.UserName == "onlyuser")).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserNameAndEmail_WhenExecuteRegisterFlow_ThenSucceedsAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "alias@example.com",
            ["UserName"] = "aliasuser",
            ["Password"] = "P@ssw0rd!",
        };

        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var userAccountId = Guid.Parse((string)payload["UserAccountId"]!);
        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.Email.Should().Be("alias@example.com");
        user.UserName.Should().Be("aliasuser");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserNameAndPhone_WhenExecuteRegisterFlow_ThenSucceedsAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["PhoneNumber"] = "+12125554321",
            ["UserName"] = "phonealias",
            ["Password"] = "P@ssw0rd!",
        };

        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var userAccountId = Guid.Parse((string)payload["UserAccountId"]!);
        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.PhoneNumber.Should().Be("+12125554321");
        user.UserName.Should().Be("phonealias");
        user.Email.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenNoIdentityFields_WhenExecuteRegisterFlow_ThenThrowsValidationExceptionAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Password"] = "P@ssw0rd!",
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*email or a phone*");
    }

    [TestCase(null, null, null)]
    [TestCase("", "", "")]
    [TestCase("   ", "  ", "\t")]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenBlankIdentityFields_WhenExecuteRegisterFlow_ThenThrowsValidationExceptionAsync(
        string? email,
        string? phone,
        string? userName)
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = email,
            ["PhoneNumber"] = phone,
            ["UserName"] = userName,
            ["Password"] = "P@ssw0rd!",
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*email or a phone*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenBlankEmailAndPhonePresent_WhenExecuteRegisterFlow_ThenStoresNullEmailAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "   ",
            ["PhoneNumber"] = "+12125559876",
            ["UserName"] = "",
            ["Password"] = "P@ssw0rd!",
        };

        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var userAccountId = Guid.Parse((string)payload["UserAccountId"]!);
        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.Email.Should().BeNull();
        user.UserName.Should().BeNull();
        user.PhoneNumber.Should().Be("+12125559876");
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
