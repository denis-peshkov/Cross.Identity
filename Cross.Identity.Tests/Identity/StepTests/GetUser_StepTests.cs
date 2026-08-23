namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class GetUser_StepTests
{
    private Faker _faker = null!;
    private Mock<ILogger> _logger = null!;

    private static Selector DefaultSelector { get; } = new();

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
        _logger = new Mock<ILogger>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenExistingUser_WhenExecuteAsync_ThenStoresUserIdAsync()
    {
        var email = _faker.Internet.Email();
        var userAccountId = Guid.NewGuid();

        var users = new Mock<IUserService>(MockBehavior.Strict);
        users.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userAccountId);

        var step = new GetUserIdStep
        {
            Kind = "lookup",
            UserService = users.Object,
            Selector = DefaultSelector,
            Logger = _logger.Object,
            Next = "done"
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "Email");
        bag.Set("collectForm.Value", email);

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Ok);
        res.Next.Should().Be("done");
        bag.Get<string>("lookup.UserId").Should().Be(userAccountId.ToString());

        users.VerifyAll();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserNotFound_WhenExecuteAsync_ThenReturnsInvalidCredentialsAsync()
    {
        var phone = _faker.Phone.PhoneNumber("+407########");
        var users = new Mock<IUserService>(MockBehavior.Strict);
        users.Setup(s => s.GetUserIdByAsync("PhoneNumber", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var step = new GetUserIdStep
        {
            Kind = "lookup",
            UserService = users.Object,
            Selector = DefaultSelector,
            Logger = _logger.Object,
            Next = null
        };

        var bag = new Bag();
        bag.Set("collectForm.Field", "PhoneNumber");
        bag.Set("collectForm.Value", phone);

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Fail);
        res.Error.Should().BeOfType<NotAuthorizedException>()
            .Which.Message.Should().Be("Invalid credentials.");

        users.VerifyAll();
    }
}
