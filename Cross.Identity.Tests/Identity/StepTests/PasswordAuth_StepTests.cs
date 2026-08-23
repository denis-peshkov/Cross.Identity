namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class PasswordAuth_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _userService = new Mock<IUserService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenValidCredentials_WhenExecuteAsync_ThenSetsUserIdAndReturnsOkAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(s => s.ValidatePasswordAsync("Email", email, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            Selector = DefaultSelector,
            PasswordKey = "collectForm.Password",
            UserIdKey = "UserId",
            Next = "token"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("token");
        bag.Get<string>("passwordAuth.UserId").Should().Be(userAccountId.ToString());
        _userService.Verify(s => s.ValidatePasswordAsync("Email", email, password, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidCredentials_WhenExecuteAsync_ThenReturnsFailAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var password = "WrongPassword";

        _userService.Setup(s => s.ValidatePasswordAsync("Email", email, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            Selector = DefaultSelector,
            PasswordKey = "collectForm.Password",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);
        bag.Set("collectForm.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Fail);
        result.Error.Should().BeOfType<NotAuthorizedException>();
        _userService.Verify(s => s.ValidatePasswordAsync("Email", email, password, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(s => s.GetUserIdByAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenAuthenticatesAndSetsUserIdAsync()
    {
        // Arrange
        var username = _faker.Internet.UserName();
        var password = "P@ssw0rd!";
        var userAccountId = Guid.NewGuid();

        _userService.Setup(s => s.ValidatePasswordAsync("UserName", username, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("UserName", username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            Selector = DefaultSelector,
            PasswordKey = "Password",
            UserIdKey = "UserId",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "UserName");
        bag.Set("collectForm.Value", username);
        bag.Set("passwordAuth.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("passwordAuth.UserId").Should().Be(userAccountId.ToString());
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenIdSelector_WhenExecuteAsync_ThenAuthenticatesAndSetsUserIdAsync()
    {
        var userAccountId = Guid.NewGuid();
        var userAccountIdText = userAccountId.ToString();
        var password = "P@ssw0rd!";

        _userService.Setup(s => s.ValidatePasswordAsync("Id", userAccountIdText, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("Id", userAccountIdText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            Selector = DefaultSelector,
            PasswordKey = "collectForm.CurrentPassword",
            UserIdKey = "UserId",
            Next = "resetPassword",
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Id");
        bag.Set("collectForm.Value", userAccountIdText);
        bag.Set("collectForm.CurrentPassword", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("passwordAuth.UserId").Should().Be(userAccountIdText);
        _userService.Verify(s => s.ValidatePasswordAsync("Id", userAccountIdText, password, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(s => s.GetUserIdByAsync("Id", userAccountIdText, It.IsAny<CancellationToken>()), Times.Once);
    }
}
