namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class SendCode_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<ICommunicationEndpointService> _communicationEndpoints = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<IHostEnvironment> _environment = null!;
    private Mock<IProcessDefinitionProvider> _processDefinitionProvider = null!;
    private Mock<ILogger> _logger = null!;
    private IConfiguration _defaultConfiguration = null!;
    private IConfiguration _developerConfiguration = null!;

    private static Selector DefaultSelector { get; } = new();

    private void SetupOtpTarget(ChannelEnum channel, string address)
    {
        _communicationEndpoints
            .Setup(c => c.ResolveOtpTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = channel, Address = address });
    }


    [SetUp]
    public void SetUp()
    {
        _communicationEndpoints = new Mock<ICommunicationEndpointService>();
        _communicationEndpoints
            .Setup(c => c.ResolveOtpTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = "default@example.com" });
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = "default@example.com" });
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
    [Category(TestCategory.UNIT)]
    public async Task GivenEmailChannel_WhenExecuteAsync_ThenSendsCodeByEmailAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = "verifyCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);

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
                It.IsAny<Guid>(),
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenTtlKeyInBag_WhenExecuteAsync_ThenUsesBagTtlAsync()
    {
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(17);

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");
        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            TtlKey = "collectForm.Ttl",
            Next = "verifyCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Ttl", ttl);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                ttl,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenTtlKeyNullInBag_WhenExecuteAsync_ThenUsesDefaultTtlAsync()
    {
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");
        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            TtlKey = "collectForm.Ttl",
            Next = "verifyCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Ttl", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenSmsChannel_WhenExecuteAsync_ThenGeneratesNumericCodeAsync()
    {
        // Arrange
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("PhoneNumber", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Sms, phone);

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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "PhoneNumber");
        bag.Set("collectForm.Value", phone);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        var code = bag.Get<string>("sendCode.LastCode");
        code.Should().NotBeNullOrEmpty();
        code.Should().MatchRegex("^[0-9]+$");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenDevelopmentEnvironmentAndSendFailure_WhenExecuteAsync_ThenReturnsFailWithoutLastCodeAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);

        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");

        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<InvalidOperationException>();
        bag.ContainsKey("sendCode.LastCode").Should().BeFalse();
        _codeService.Verify(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNotFound_WhenExecuteAsync_ThenReturnsInvalidCredentialsFailureAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>()
            .Which.Message.Should().Be("Invalid credentials.");
        _codeService.Verify(
            s => s.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenKnownUserWithoutOtpChannel_WhenExecuteAsync_ThenReturnsInvalidCredentialsAsync()
    {
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        _communicationEndpoints
            .Setup(c => c.ResolveOtpTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("No preferred verified communication channel and no email."));

        var step = new SendCodeStep
        {
            Kind = "sendCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Environment = _environment.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Configuration = _defaultConfiguration,
            Logger = _logger.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>()
            .Which.Message.Should().Be("Invalid credentials.");
        _codeService.Verify(
            s => s.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenResetTemplate_WhenExecuteAsync_ThenUsesResetCopyAndEmailInUrlAsync()
    {
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "txt"))
            .Returns("Reset {{email}} {{code}} {{url}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("reset", "en", "html"))
            .Returns("<html>{{email}} {{url}}</html>");
        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            Configuration = _developerConfiguration,
            Logger = _logger.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "reset",
            Subject = "Reset your password",
            Selector = DefaultSelector,
            Next = "collectResult",
        };

        var bag = new Bag()
            .Set("collectForm.Field", "Email")
            .Set("collectForm.Value", email);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Subject == "Reset your password"
                    && m.TextBody!.Contains(email)
                    && m.TextBody.Contains("http://localhost:4200/reset-password?code=")
                    && m.TextBody.Contains("email=")),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNameSelector_WhenExecuteAsync_ThenSendsToPreferredEndpointAddressAsync()
    {
        var userName = "alice";
        var email = "alice@example.com";
        var userId = Guid.NewGuid();

        _userService.Setup(s => s.GetUserIdByAsync("UserName", userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _environment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("Your code: {{code}}");
        _processDefinitionProvider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<html>Your code: {{code}}</html>");
        _codeService.Setup(c => c.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                It.IsAny<Guid>(),
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
            CommunicationEndpoints = _communicationEndpoints.Object,
            Template = "verify",
            Subject = "Verification Code",
            Selector = DefaultSelector,
            Next = "verifyCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "UserName");
        bag.Set("collectForm.Value", userName);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Channel == ChannelEnum.Email &&
                    m.Destination == email),
                It.IsAny<string>(),
                userId,
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
