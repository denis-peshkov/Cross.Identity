namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class SendCode_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<IHostEnvironment> _environment = null!;
    private Mock<IProcessDefinitionProvider> _processDefinitionProvider = null!;
    private Mock<ILogger> _logger = null!;
    private IConfiguration _defaultConfiguration = null!;
    private IConfiguration _developerConfiguration = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _codeService = new Mock<ICodeService>();
        _userService = new Mock<IUserService>();
        _environment = new Mock<IHostEnvironment>();
        _processDefinitionProvider = new Mock<IProcessDefinitionProvider>();
        _logger = new Mock<ILogger>();
        _defaultConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:ClientUrl"] = "http://localhost:4200",
                ["Authentication:DeveloperMode"] = "false"
            })
            .Build();
        _developerConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:ClientUrl"] = "http://localhost:4200",
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
    }

    [Test]
    public async Task SendCodeStep_ShouldSendCodeByEmail()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid().ToString();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new SendCodeStep
        {
            Kind = "sendCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Configuration = _defaultConfiguration,
            Logger = _logger.Object,
            Channel = ChannelEnum.Email,
            SelectorKey = "collectForm.Email",
            ResolveBy = new ResolveBy { Field = "Email" },
            Ttl = TimeSpan.FromMinutes(5),
            Next = "verifyCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("verifyCode");
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
    public async Task SendCodeStep_ShouldGenerateNumericCodeForSms()
    {
        // Arrange
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var userId = Guid.NewGuid().ToString();

        _userService.Setup(s => s.GetUserIdByAsync("Phone", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Code: {{code}}</html>");

        var step = new SendCodeStep
        {
            Kind = "sendCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Configuration = _developerConfiguration,
            Logger = _logger.Object,
            Channel = ChannelEnum.Sms,
            SelectorKey = "collectForm.Phone",
            ResolveBy = new ResolveBy { Field = "Phone" },
            Ttl = TimeSpan.FromMinutes(5),
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Phone", phone);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        var code = bag.Get<string>("sendCode.LastCode");
        code.Should().NotBeNullOrEmpty();
        code.Should().MatchRegex("^[0-9]+$"); // Проверяем, что код только из цифр для SMS
    }

    [Test]
    public async Task SendCodeStep_InDevelopment_ShouldNotSetLastCodeWhenSendFails()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid().ToString();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var step = new SendCodeStep
        {
            Kind = "sendCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Configuration = _developerConfiguration,
            Logger = _logger.Object,
            Channel = ChannelEnum.Email,
            SelectorKey = "collectForm.Email",
            ResolveBy = new ResolveBy { Field = "Email" },
            Ttl = TimeSpan.FromMinutes(5),
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.ContainsKey("sendCode.LastCode").Should().BeFalse();
        _codeService.Verify(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SendCodeStep_ShouldHandleUserNotFound()
    {
        // Arrange
        var email = _faker.Internet.Email();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("User not found"));

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Test template");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Test template</html>");

        var step = new SendCodeStep
        {
            Kind = "sendCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Configuration = _defaultConfiguration,
            Logger = _logger.Object,
            Channel = ChannelEnum.Email,
            SelectorKey = "collectForm.Email",
            ResolveBy = new ResolveBy { Field = "Email" },
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);

        // Act & Assert
        var act = async () => await step.ExecuteAsync(bag, CancellationToken.None);
        await act.Should()
            .ThrowAsync<NotFoundException>();
    }
}
