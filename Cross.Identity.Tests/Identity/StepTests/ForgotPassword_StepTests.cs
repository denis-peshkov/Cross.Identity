namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ForgotPassword_StepTests
{
    private Mock<ILogger> _logger = null!;
    private Mock<ICodeService> _codeService = null!;
    private IConfiguration _defaultConfiguration = null!;
    private IConfiguration _developerConfiguration = null!;
    private Mock<IHostEnvironment> _environment = null!;
    private Mock<IProcessDefinitionProvider> _processDefinitionProvider = null!;
    private ForgotPasswordStep _step = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _logger = new Mock<ILogger>();
        _codeService = new Mock<ICodeService>();
        _defaultConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "false",
            })
            .Build();
        _developerConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true",
            })
            .Build();
        _environment = new Mock<IHostEnvironment>();
        _processDefinitionProvider = new Mock<IProcessDefinitionProvider>();

        _step = CreateStep(_developerConfiguration);
    }

    private ForgotPasswordStep CreateStep(IConfiguration configuration)
        => new()
        {
            Kind = "forgotPassword",
            SelectorKey = "email",
            PasswordKey = "password",
            Channel = ChannelEnum.Email,
            Logger = _logger.Object,
            CodeService = _codeService.Object,
            Configuration = configuration,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Next = "nextStep"
        };

    [Test]
    public async Task GivenEmailChannel_WhenExecuteAsync_ThenSendsResetEmailAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var bag = new Bag().Set("forgotPassword.email", email);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Test {{email}} {{code}} {{expires}} {{url}} {{year}} {{brand}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>{{email}} {{code}} {{expires}} {{url}} {{year}} {{brand}}</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        bag.Get<string>("forgotPassword.LastCode").Should().NotBeNullOrEmpty();
        _codeService.Verify(c => c.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Channel == ChannelEnum.Email &&
                    m.Destination == email),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GivenSmsChannel_WhenExecuteAsync_ThenGeneratesNumericCodeAsync()
    {
        // Arrange
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var bag = new Bag().Set("forgotPassword.email", phone);

        _step.Channel = ChannelEnum.Sms;
        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>Code: {{code}}</html>");

        // Act
        var result = await _step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        var code = bag.Get<string>("forgotPassword.LastCode");
        code.Should().NotBeNullOrEmpty();
        code.Should().MatchRegex("^[0-9]+$"); // Verify that the code contains only digits
    }

    [Test]
    public async Task GivenDeveloperModeDisabled_WhenExecuteAsync_ThenDoesNotSetLastCodeAsync()
    {
        var email = _faker.Internet.Email();
        var bag = new Bag().Set("forgotPassword.email", email);
        var step = CreateStep(_defaultConfiguration);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Test template");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>Test template</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.ContainsKey("forgotPassword.LastCode").Should().BeFalse();
    }

    [Test]
    public async Task GivenDevelopmentEnvironment_WhenExecuteAsync_ThenDoesNotSendNotificationAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var bag = new Bag().Set("forgotPassword.email", email);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Test template");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>Test template</html>");

        // Act
        var result = await _step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GivenCodeServiceFailure_WhenExecuteAsync_ThenLogsErrorAndReturnsOkAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var bag = new Bag().Set("forgotPassword.email", email);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Test template");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>Test template</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        var result = await _step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        _logger.Verify(l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
