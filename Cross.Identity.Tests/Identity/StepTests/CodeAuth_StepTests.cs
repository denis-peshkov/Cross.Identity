namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class CodeAuth_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<IUserService> _userService = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _codeService = new Mock<ICodeService>();
        _userService = new Mock<IUserService>();
    }

    [Test]
    public async Task CodeAuthStep_WhenCodeValid_ShouldSetUserIdAndReturnOk()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";
        var userId = Guid.NewGuid().ToString();

        _codeService.Setup(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = "email",
            IdentityKey = "auth-form.Email",
            CodeKey = "auth-form.Code",
            ResolveBy = new ResolveBy { Field = "Email" },
            UserIdKey = "UserId",
            Next = "nextStep"
        };

        var bag = new Bag();
        bag.Set("auth-form.Email", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        bag.Get<string>("codeAuth.UserId").Should().Be(userId);
        _codeService.Verify(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CodeAuthStep_WhenCodeInvalid_ShouldReturnFail()
    {
        var email = _faker.Internet.Email();
        var code = "WRONG";

        _codeService.Setup(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = "email",
            IdentityKey = "auth-form.Email",
            CodeKey = "auth-form.Code",
            ResolveBy = new ResolveBy { Field = "Email" },
            Next = null
        };

        var bag = new Bag();
        bag.Set("auth-form.Email", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>();
        _userService.Verify(u => u.GetUserIdByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CodeAuthStep_WhenUserNotFound_ShouldReturnFail()
    {
        var email = _faker.Internet.Email();
        var code = "ABC123";

        _codeService.Setup(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = "email",
            IdentityKey = "auth-form.Email",
            CodeKey = "auth-form.Code",
            ResolveBy = new ResolveBy { Field = "Email" },
            Next = null
        };

        var bag = new Bag();
        bag.Set("auth-form.Email", email);
        bag.Set("auth-form.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<KeyNotFoundException>();
    }

    [Test]
    public async Task CodeAuthStep_WithRelativeKeys_ShouldQualifyKeys()
    {
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var code = "123456";
        var userId = Guid.NewGuid().ToString();

        _codeService.Setup(c => c.VerifyAsync("phone", phone, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(u => u.GetUserIdByAsync("Phone", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CodeAuthStep
        {
            Kind = "codeAuth",
            CodeService = _codeService.Object,
            UserService = _userService.Object,
            Channel = "phone",
            IdentityKey = "Phone",
            CodeKey = "Code",
            ResolveBy = new ResolveBy { Field = "Phone" },
            Next = null
        };

        var bag = new Bag();
        bag.Set("codeAuth.Phone", phone);
        bag.Set("codeAuth.Code", code);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("codeAuth.Id").Should().Be(userId);
    }
}
