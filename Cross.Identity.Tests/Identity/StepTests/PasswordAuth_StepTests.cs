namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class PasswordAuth_StepTests
{
    private Faker _faker = null!;
    private Mock<IUserService> _userService = null!;

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
        var userId = Guid.NewGuid().ToString();

        _userService.Setup(s => s.ValidatePasswordAsync("Email", email, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            SelectorField = "Email",
            SelectorKey = "collectForm.Email",
            PasswordKey = "collectForm.Password",
            UserIdKey = "UserId",
            Next = "token"
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
        bag.Set("collectForm.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("token");
        bag.Get<string>("passwordAuth.UserId").Should().Be(userId);
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
            SelectorField = "Email",
            SelectorKey = "collectForm.Email",
            PasswordKey = "collectForm.Password",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
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
        var userId = Guid.NewGuid().ToString();

        _userService.Setup(s => s.ValidatePasswordAsync("UserName", username, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("UserName", username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            SelectorField = "UserName",
            SelectorKey = "Email", // relative key
            PasswordKey = "Password", // relative key
            UserIdKey = "UserId",
            Next = null
        };

        var bag = new Bag();
        bag.Set("passwordAuth.Email", username);
        bag.Set("passwordAuth.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("passwordAuth.UserId").Should().Be(userId);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenIdSelector_WhenExecuteAsync_ThenAuthenticatesAndSetsUserIdAsync()
    {
        var userId = Guid.NewGuid().ToString();
        var password = "P@ssw0rd!";

        _userService.Setup(s => s.ValidatePasswordAsync("Id", userId, password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userService.Setup(s => s.GetUserIdByAsync("Id", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new PasswordAuthStep
        {
            Kind = "passwordAuth",
            UserService = _userService.Object,
            SelectorField = "Id",
            SelectorKey = "collectForm.UserId",
            PasswordKey = "collectForm.CurrentPassword",
            UserIdKey = "UserId",
            Next = "resetPassword",
        };

        var bag = new Bag();
        bag.Set("collectForm.UserId", userId);
        bag.Set("collectForm.CurrentPassword", password);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("passwordAuth.UserId").Should().Be(userId);
        _userService.Verify(s => s.ValidatePasswordAsync("Id", userId, password, It.IsAny<CancellationToken>()), Times.Once);
        _userService.Verify(s => s.GetUserIdByAsync("Id", userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
