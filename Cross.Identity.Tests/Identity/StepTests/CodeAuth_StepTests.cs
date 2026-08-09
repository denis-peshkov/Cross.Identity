namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class CodeAuth_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<IUserService> _userService = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _codeService = new Mock<ICodeService>();
        _userService = new Mock<IUserService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidCode_WhenExecuteAsync_ThenSetsUserIdAndReturnsOkAsync()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";
        var userId = Guid.NewGuid().ToString();

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = ChannelEnum.Email,
            Selector = DefaultSelector,
            CodeKey = "auth-form.Code",
            UserIdKey = "UserId",
            Next = "nextStep"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        bag.Get<string>("codeAuth.UserId").Should().Be(userId);
        _codeService.Verify(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidCode_WhenExecuteAsync_ThenReturnsFailAsync()
    {
        var email = _faker.Internet.Email();
        var code = "WRONG";

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = ChannelEnum.Email,
            Selector = DefaultSelector,
            CodeKey = "auth-form.Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>();
        _userService.Verify(u => u.GetUserIdByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNotFound_WhenExecuteAsync_ThenReturnsFailAsync()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = ChannelEnum.Email,
            Selector = DefaultSelector,
            CodeKey = "auth-form.Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<KeyNotFoundException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenQualifiesKeysAndSetsUserIdAsync()
    {
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var code = "123456";
        var userId = Guid.NewGuid().ToString();

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Sms, phone, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("PhoneNumber", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = ChannelEnum.Sms,
            Selector = DefaultSelector,
            CodeKey = "Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "PhoneNumber");
        bag.Set("collectForm.Value", phone);
        bag.Set("codeAuth.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("codeAuth.Id").Should().Be(userId);
    }
}
