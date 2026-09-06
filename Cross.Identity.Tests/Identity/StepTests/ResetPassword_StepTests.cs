namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ResetPassword_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<ICommunicationEndpointService> _communicationEndpoints = null!;
    private Mock<IEmailSenderService> _emailSenderService = null!;
    private Mock<ISmsSenderService> _smsSenderService = null!;
    private Mock<IProcessDefinitionProvider> _processDefinitionProvider = null!;
    private Mock<ILogger> _logger = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _communicationEndpoints = new Mock<ICommunicationEndpointService>();
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = "notify@example.com" });
        _faker = new Faker();
        _userService = new Mock<IUserService>();
        _emailSenderService = new Mock<IEmailSenderService>();
        _smsSenderService = new Mock<ISmsSenderService>();
        _processDefinitionProvider = new Mock<IProcessDefinitionProvider>();
        _processDefinitionProvider
            .Setup(p => p.GetTemplate("password-changed", "en", "txt"))
            .Returns("Password changed at {{changedAt}} from IP {{ip}}. Support: {{support}}");
        _processDefinitionProvider
            .Setup(p => p.GetTemplate("password-changed", "en", "html"))
            .Returns("<p>Password changed at <strong>{{changedAt}}</strong> from IP <strong>{{ip}}</strong>.</p>");
        _logger = new Mock<ILogger>();
    }

    private ResetPasswordStep CreateStep(string passwordKey = "forgotPassword.password", string? next = "done")
        => new()
        {
            Kind = "resetPassword",
            Selector = DefaultSelector,
            PasswordKey = passwordKey,
            Template = "password-changed",
            Subject = "Password changed",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            ProcessDefinitionProvider = _processDefinitionProvider.Object,
            Notifications = new NotificationOptions(),
            Logger = _logger.Object,
            Next = next,
        };

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenEmailAndPassword_WhenExecuteAsync_ThenSetsPasswordAndReturnsNextAsync()
    {
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";

        _userService.Setup(u => u.SetPasswordAsync("Email", email, password, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = email });

        var step = CreateStep();

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("forgotPassword.password", password);
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Email", email, password, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSenderService.Verify(
            x => x.SendAsync(
                "",
                email,
                "Password changed",
                It.Is<string>(b => b.Contains("from IP unknown")),
                It.Is<string>(b => b.Contains("<strong>unknown</strong>")),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _processDefinitionProvider.Verify(p => p.GetTemplate("password-changed", "en", "txt"), Times.Once);
        _processDefinitionProvider.Verify(p => p.GetTemplate("password-changed", "en", "html"), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenLanguageCodeRu_WhenExecuteAsync_ThenLoadsRuTemplateAsync()
    {
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";

        _processDefinitionProvider
            .Setup(p => p.GetTemplate("password-changed", "ru", "txt"))
            .Returns("Пароль изменён в {{changedAt}} с IP {{ip}}. Поддержка: {{support}}");
        _processDefinitionProvider
            .Setup(p => p.GetTemplate("password-changed", "ru", "html"))
            .Returns("<p>Пароль изменён в <strong>{{changedAt}}</strong> с IP <strong>{{ip}}</strong>.</p>");

        _userService.Setup(u => u.SetPasswordAsync("Email", email, password, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = email });

        var step = CreateStep();

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("forgotPassword.password", password);
        bag.Set("collectForm.LanguageCode", "ru");
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        _processDefinitionProvider.Verify(p => p.GetTemplate("password-changed", "ru", "txt"), Times.Once);
        _processDefinitionProvider.Verify(p => p.GetTemplate("password-changed", "ru", "html"), Times.Once);
        _processDefinitionProvider.Verify(p => p.GetTemplate("password-changed", "en", "txt"), Times.Never);
        _emailSenderService.Verify(
            x => x.SendAsync(
                "",
                email,
                "Password changed",
                It.Is<string>(b => b.StartsWith("Пароль изменён")),
                It.Is<string>(b => b.Contains("Пароль изменён")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdSelector_WhenExecuteAsync_ThenSetsPasswordAndNotifiesResolvedTargetAsync()
    {
        var userAccountId = Guid.NewGuid();
        var password = "P@ssw0rd!";
        var userAccountIdText = userAccountId.ToString();

        _userService.Setup(u => u.SetPasswordAsync("Id", userAccountIdText, password, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserAccountIdByAsync("Id", userAccountIdText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = CreateStep(passwordKey: "collectForm.NewPassword");

        var bag = new Bag();
        bag.Set("collectForm.Field", "Id");
        bag.Set("collectForm.Value", userAccountIdText);
        bag.Set("collectForm.NewPassword", password);
        bag.Set("collectForm.IpAddress", "10.0.0.1");
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Id", userAccountIdText, password, It.IsAny<HostSuppliedClientContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _userService.Verify(
            u => u.GetUserByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailSenderService.Verify(
            x => x.SendAsync(
                "",
                "notify@example.com",
                "Password changed",
                It.Is<string>(b => b.Contains("from IP 10.0.0.1")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
