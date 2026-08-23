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
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = "notify@example.com" });
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

        _userService.Setup(u => u.SetPasswordAsync("Email", email, password, ClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());
        _communicationEndpoints
            .Setup(c => c.ResolveDeliveryTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = ChannelEnum.Email, Address = email });

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            Selector = DefaultSelector,
            PasswordKey = "forgotPassword.password",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Logger = _logger.Object,
            Next = "done"
        };

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
            u => u.SetPasswordAsync("Email", email, password, ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSenderService.Verify(
            x => x.SendAsync("", email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdSelector_WhenExecuteAsync_ThenSetsPasswordAndNotifiesResolvedTargetAsync()
    {
        var userAccountId = Guid.NewGuid();
        var password = "P@ssw0rd!";
        var userAccountIdText = userAccountId.ToString();

        _userService.Setup(u => u.SetPasswordAsync("Id", userAccountIdText, password, ClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userService.Setup(u => u.GetUserIdByAsync("Id", userAccountIdText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            Selector = DefaultSelector,
            PasswordKey = "collectForm.NewPassword",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Logger = _logger.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Id");
        bag.Set("collectForm.Value", userAccountIdText);
        bag.Set("collectForm.NewPassword", password);
        bag.Set("collectForm.IpAddress", null);
        bag.Set("collectForm.UserAgent", null);
        bag.Set("collectForm.DeviceFingerprint", null);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Id", userAccountIdText, password, ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
        _userService.Verify(
            u => u.GetUserByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailSenderService.Verify(
            x => x.SendAsync("", "notify@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
