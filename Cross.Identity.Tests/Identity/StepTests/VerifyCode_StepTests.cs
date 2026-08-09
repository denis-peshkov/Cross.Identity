namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class VerifyCode_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;
    private Mock<IUserService> _userService = null!;
    private Mock<ICommunicationEndpointService> _communicationEndpoints = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _userService = new Mock<IUserService>();
        _userService.Setup(u => u.GetUserIdByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid().ToString());
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
        _codeService = new Mock<ICodeService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidCode_WhenExecuteAsync_ThenReturnsOkAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var code = "ABC123";

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = ChannelEnum.Email,
            Selector = DefaultSelector,
            CodeKey = "collectForm.Code",
            Next = "nextStep"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        _codeService.Verify(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidCode_WhenExecuteAsync_ThenReturnsFailAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var code = "INVALID";

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = ChannelEnum.Email,
            Selector = DefaultSelector,
            CodeKey = "collectForm.Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>();
        _codeService.Verify(c => c.VerifyAsync(ChannelEnum.Email, email, code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenVerifiesCodeAsync()
    {
        // Arrange
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var code = "123456";

        _codeService.Setup(c => c.VerifyAsync(ChannelEnum.Sms, phone, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = ChannelEnum.Sms,
            Selector = DefaultSelector,
            CodeKey = "Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "PhoneNumber");
        bag.Set("collectForm.Value", phone);
        bag.Set("verifyCode.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.VerifyAsync(ChannelEnum.Sms, phone, code, It.IsAny<CancellationToken>()), Times.Once);
    }
}
