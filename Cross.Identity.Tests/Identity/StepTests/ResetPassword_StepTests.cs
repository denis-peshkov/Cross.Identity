namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ResetPassword_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<ICommunicationEndpointService> _communicationEndpoints = null!;
    private Mock<IEmailSenderService> _emailSenderService = null!;
    private Mock<ISmsSenderService> _smsSenderService = null!;
    private Mock<ILogger> _logger = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _communicationEndpoints = new Mock<ICommunicationEndpointService>();
        _communicationEndpoints
            .Setup(c => c.ResolveOtpChannelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChannelEnum?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string field, string __, ChannelEnum? fallback, CancellationToken ___) =>
                field.Equals("PhoneNumber", StringComparison.OrdinalIgnoreCase) ? ChannelEnum.Sms : (fallback ?? ChannelEnum.Email));
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryChannelAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ChannelEnum?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, string field, string __, ChannelEnum? fallback, CancellationToken ___) =>
                field.Equals("PhoneNumber", StringComparison.OrdinalIgnoreCase) ? ChannelEnum.Sms : (fallback ?? ChannelEnum.Email));
        _communicationEndpoints
            .Setup(c => c.GetPreferredAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CommunicationEndpointDto?)null);
        _faker = new Faker();
        _userService = new Mock<IUserService>();
        _emailSenderService = new Mock<IEmailSenderService>();
        _smsSenderService = new Mock<ISmsSenderService>();
        _logger = new Mock<ILogger>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenEmailAndPassword_WhenExecuteAsync_ThenSetsPasswordAndReturnsNextAsync()
    {
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";

        _userService.Setup(u => u.SetPasswordAsync("Email", email, password, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            Selector = DefaultSelector,
            PasswordKey = "forgotPassword.password",
            IpAddressKey = "IpAddress",
            UserAgentKey = "UserAgent",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Channel = ChannelEnum.Email,
            Logger = _logger.Object,
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("forgotPassword.password", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Email", email, password, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSenderService.Verify(
            x => x.SendAsync("", email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdSelector_WhenExecuteAsync_ThenSetsPasswordAndNotifiesWithSelectorValueAsync()
    {
        var userId = Guid.NewGuid();
        var password = "P@ssw0rd!";
        var userIdText = userId.ToString();

        _userService.Setup(u => u.SetPasswordAsync("Id", userIdText, password, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserIdByAsync("Id", userIdText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userIdText);

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            Selector = DefaultSelector,
            PasswordKey = "collectForm.NewPassword",
            IpAddressKey = "IpAddress",
            UserAgentKey = "UserAgent",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Channel = ChannelEnum.Email,
            Logger = _logger.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Id");
        bag.Set("collectForm.Value", userIdText);
        bag.Set("collectForm.NewPassword", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Id", userIdText, password, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
        _userService.Verify(
            u => u.GetUserByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailSenderService.Verify(
            x => x.SendAsync("", userIdText, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
