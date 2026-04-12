namespace Cross.Identity.UnitTests.Identity.StepTests;

[TestFixture]
public class GetUser_StepTests
{
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _faker = new Faker();
    }

    [Test]
    public async Task GetUserStep_ShouldPutUserId_WhenFound()
    {
        var email = _faker.Internet.Email();
        var userId = Guid.NewGuid().ToString("N");

        var users = new Mock<IUserService>(MockBehavior.Strict);
        users.Setup(s => s.GetUserIdByAsync("Email", email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);

        var step = new GetUserIdStep
        {
            Kind = "lookup",
            UserService = users.Object,
            SelectorField = "Email",
            SelectorKey = "get.Email",
            Next = "done"
        };

        var bag = new Bag().Set("get.Email", email);

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Ok);
        res.Next.Should().Be("done");
        bag.Get<string>("lookup.UserId").Should().Be(userId);

        users.VerifyAll();
    }

    [Test]
    public async Task GetUserStep_ShouldFail_WhenNotFound()
    {
        var phone = _faker.Phone.PhoneNumber("+407########");
        var users = new Mock<IUserService>(MockBehavior.Strict);
        users.Setup(s => s.GetUserIdByAsync("Phone", phone, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var step = new GetUserIdStep
        {
            Kind = "lookup",
            UserService = users.Object,
            SelectorField = "Phone",
            SelectorKey = "get.Phone",
            Next = null
        };

        var bag = new Bag().Set("get.Phone", phone);

        var res = await step.ExecuteAsync(bag, CancellationToken.None);

        res.Status.Should().Be(StepStatusEnum.Fail);
        res.Error.Should().BeOfType<KeyNotFoundException>();

        users.VerifyAll();
    }
}
