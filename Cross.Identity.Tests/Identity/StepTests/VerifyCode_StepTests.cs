namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class VerifyCode_StepTests
{
    private Faker _faker = null!;
    private Mock<ICodeService> _codeService = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _codeService = new Mock<ICodeService>();
    }

    [Test]
    public async Task VerifyCodeStep_ShouldVerifyValidCode()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var code = "ABC123";

        _codeService.Setup(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = "email",
            IdentityKey = "collectForm.Email",
            CodeKey = "collectForm.Code",
            Next = "nextStep"
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
        bag.Set("collectForm.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("nextStep");
        _codeService.Verify(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task VerifyCodeStep_ShouldFailOnInvalidCode()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var code = "INVALID";

        _codeService.Setup(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = "email",
            IdentityKey = "collectForm.Email",
            CodeKey = "collectForm.Code",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
        bag.Set("collectForm.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>();
        _codeService.Verify(c => c.VerifyAsync("email", email, code, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task VerifyCodeStep_ShouldHandleRelativeKeys()
    {
        // Arrange
        var phone = _faker.Phone.PhoneNumber("+1##########");
        var code = "123456";

        _codeService.Setup(c => c.VerifyAsync("phone", phone, code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var step = new VerifyCodeStep
        {
            Kind = "verifyCode",
            CodeService = _codeService.Object,
            Channel = "phone",
            IdentityKey = "Phone", // relative key
            CodeKey = "Code", // relative key
            Next = null
        };

        var bag = new Bag();
        bag.Set("verifyCode.Phone", phone);
        bag.Set("verifyCode.Code", code);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        _codeService.Verify(c => c.VerifyAsync("phone", phone, code, It.IsAny<CancellationToken>()), Times.Once);
    }
}
