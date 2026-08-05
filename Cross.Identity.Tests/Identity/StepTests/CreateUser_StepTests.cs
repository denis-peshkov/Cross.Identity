namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class CreateUser_StepTests
{
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
    }

    [Test]
    public async Task GivenUserDataInBag_WhenExecuteAsync_ThenCreatesUserAndStoresUserIdAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";
        var userId = Guid.NewGuid().ToString();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "collectForm.Email",
                ["Password"] = "collectForm.Password"
            },
            UserIdKey = "UserId",
            SelectorKey = "collectForm.Email",
            Next = "sendCode"
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
        bag.Set("collectForm.Password", password);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("sendCode");
        bag.Get<string>("createUser.UserId").Should().Be(userId);
        bag.Get<string>("createUser.selectorKey").Should().Be(email);
        userService.Verify(s => s.CreateUserAsync(
            It.Is<IDictionary<string, object?>>(m =>
                m.ContainsKey("Email") &&
                m.ContainsKey("Password") &&
                Equals(m["Email"], email) &&
                Equals(m["Password"], password)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenCreatesUserAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid().ToString();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "Email" // relative key
            },
            UserIdKey = "UserId",
            SelectorKey = "Email",
            Next = null
        };

        var bag = new Bag();
        bag.Set("createUser.Email", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("createUser.UserId").Should().Be(userId);
        userService.Verify(s => s.CreateUserAsync(
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GivenMissingMapValues_WhenExecuteAsync_ThenCreatesUserWithAvailableFieldsAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid().ToString();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "collectForm.Email",
                ["Password"] = "collectForm.Password" // missing from Bag
            },
            UserIdKey = "UserId",
            SelectorKey = "collectForm.Email",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);
        // Password is missing

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("createUser.UserId").Should().Be(userId);
        userService.Verify(s => s.CreateUserAsync(
            It.Is<IDictionary<string, object?>>(m =>
                m.ContainsKey("Email") &&
                !m.ContainsKey("Password") &&
                Equals(m["Email"], email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
