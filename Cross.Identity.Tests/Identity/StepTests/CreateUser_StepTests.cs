namespace Cross.Identity.Tests.Identity.StepTests;

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
    [Category(TestCategory.UNIT)]
    public async Task GivenUserDataInBag_WhenExecuteAsync_ThenCreatesUserAndStoresUserIdAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var password = "P@ssw0rd!";
        var userAccountId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "collectForm.Email",
                ["Password"] = "collectForm.Password"
            },
            UserAccountIdKey = "UserAccountId",
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
        bag.Get<string>("createUser.UserAccountId").Should().Be(userAccountId.ToString());
        userService.Verify(s => s.CreateUserAsync(
            It.Is<IDictionary<string, object?>>(m =>
                m.ContainsKey("Email") &&
                m.ContainsKey("Password") &&
                Equals(m["Email"], email) &&
                Equals(m["Password"], password)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenRelativeKeys_WhenExecuteAsync_ThenCreatesUserAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userAccountId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "Email"
            },
            UserAccountIdKey = "UserAccountId",
            Next = null
        };

        var bag = new Bag();
        bag.Set("createUser.Email", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("createUser.UserAccountId").Should().Be(userAccountId.ToString());
        userService.Verify(s => s.CreateUserAsync(
            It.IsAny<IDictionary<string, object?>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenMissingMapValues_WhenExecuteAsync_ThenCreatesUserWithAvailableFieldsAsync()
    {
        // Arrange
        var email = _faker.Internet.Email();
        var userAccountId = Guid.NewGuid();

        var userService = new Mock<IUserService>(MockBehavior.Strict);
        userService.Setup(s => s.CreateUserAsync(
                It.IsAny<IDictionary<string, object?>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new CreateUserStep
        {
            Kind = "createUser",
            UserService = userService.Object,
            Map = new Dictionary<string, string>
            {
                ["Email"] = "collectForm.Email",
                ["Password"] = "collectForm.Password"
            },
            UserAccountIdKey = "UserAccountId",
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Email", email);

        // Act
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        // Assert
        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<string>("createUser.UserAccountId").Should().Be(userAccountId.ToString());
        userService.Verify(s => s.CreateUserAsync(
            It.Is<IDictionary<string, object?>>(m =>
                m.ContainsKey("Email") &&
                !m.ContainsKey("Password") &&
                Equals(m["Email"], email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
