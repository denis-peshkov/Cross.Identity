namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class VerifyCode_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<ICommunicationEndpointService> _communicationEndpoints = null!;

    private Mock<ILogger> _logger = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _codeService = new Mock<ICodeService>();
        _userService = new Mock<IUserService>();
        _communicationEndpoints = new Mock<ICommunicationEndpointService>();
        _logger = new Mock<ILogger>();
    }

    private VerifyCodeStep CreateStep(
        string codeKey = "collectForm.Code",
        string? userAccountIdKey = "UserAccountId",
        string? next = "nextStep")
        => new()
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            CommunicationEndpoints = _communicationEndpoints.Object,
            Logger = _logger.Object,
            Selector = DefaultSelector,
            CodeKey = codeKey,
            UserAccountIdKey = userAccountIdKey ?? "UserAccountId",
            Next = next,
        };

    private void SetupOtpTarget(ChannelEnum channel, string address)
    {
        _communicationEndpoints
            .Setup(c => c.ResolveOtpTargetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DeliveryTarget { Channel = channel, Address = address });
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidCode_WhenExecuteAsync_ThenSetsUserIdAndReturnsOkAsync()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _codeService.Setup(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = CreateStep();

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        bag.Get<string>("verifyCode.UserAccountId").Should().Be(userAccountId.ToString());
        _codeService.Verify(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidCode_WhenExecuteAsync_ThenReturnsFailAsync()
    {
        var email = _faker.Internet.Email();
        var code = "INVALID";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _codeService.Setup(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = CreateStep(next: null);

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>()
            .Which.Message.Should().Be("Invalid credentials.");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNotFound_WhenExecuteAsync_ThenReturnsInvalidCredentialsAsync()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";

        _userService.Setup(u => u.GetUserAccountIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var step = CreateStep(next: null);

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>()
            .Which.Message.Should().Be("Invalid credentials.");
        _codeService.Verify(
            c => c.VerifyAsync(It.IsAny<Guid>(), It.IsAny<ChannelEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenQualifiesKeysAndSetsUserIdAsync()
    {
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var code = "123456";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(u => u.GetUserAccountIdByAsync("PhoneNumber", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);
        SetupOtpTarget(ChannelEnum.Sms, phone);
        _codeService.Setup(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Sms, phone, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = CreateStep(codeKey: "Code", next: null);

        var bag = new Bag();
        bag.Set("collectForm.Field", "PhoneNumber");
        bag.Set("collectForm.Value", phone);
        bag.Set("verifyCode.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("verifyCode.UserAccountId").Should().Be(userAccountId.ToString());
        _codeService.Verify(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Sms, phone, code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNameSelector_WhenExecuteAsync_ThenVerifiesAgainstResolvedTargetAsync()
    {
        var userName = "alice";
        var email = "alice@example.com";
        var code = "ABC123";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(u => u.GetUserAccountIdByAsync("UserName", userName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);
        SetupOtpTarget(ChannelEnum.Email, email);
        _codeService.Setup(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = CreateStep(next: null);

        var bag = new Bag();
        bag.Set("collectForm.Field", "UserName");
        bag.Set("collectForm.Value", userName);
        bag.Set("collectForm.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("verifyCode.UserAccountId").Should().Be(userAccountId.ToString());
        _codeService.Verify(c => c.VerifyAsync(It.IsAny<Guid>(), ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(u => u.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
