namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ResetPassword_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<IEmailSenderService> _emailSenderService = null!;
    private Mock<ISmsSenderService> _smsSenderService = null!;
    private Mock<IHttpContextAccessor> _httpContextAccessor = null!;
    private Mock<ILogger> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _userService = new Mock<IUserService>();
        _emailSenderService = new Mock<IEmailSenderService>();
        _smsSenderService = new Mock<ISmsSenderService>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _logger = new Mock<ILogger>();
    }

    [Test]
    public async Task ResetPasswordStep_ShouldSetPasswordAndReturnNext()
    {
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";

        _userService.Setup(u => u.SetPasswordAsync("Email", email, password, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new ResetPasswordStep
        {
            Kind = "resetPassword",
            SelectorKey = "forgotPassword.email",
            PasswordKey = "forgotPassword.password",
            UserService = _userService.Object,
            EmailSenderService = _emailSenderService.Object,
            SmsSenderService = _smsSenderService.Object,
            HttpContextAccessor = _httpContextAccessor.Object,
            Channel = ChannelEnum.Email,
            Logger = _logger.Object,
            ResolveBy = new ResolveBy { Field = "Email" },
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("forgotPassword.email", email);
        bag.Set("forgotPassword.password", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        _userService.Verify(
            u => u.SetPasswordAsync("Email", email, password, It.IsAny<CancellationToken>()),
            Times.Once);
        _emailSenderService.Verify(
            x => x.SendAsync("", email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
