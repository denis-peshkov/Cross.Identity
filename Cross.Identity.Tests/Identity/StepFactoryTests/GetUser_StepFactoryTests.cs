namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class GetUser_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IUserService>(p => Mock.Of<IUserService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserId",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email",
              "next": "token"
            }
            """);

        var factory = new GetUserIdStepFactory();

        // Act
        var step = (GetUserIdStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("getUserId");
        step.SelectorField.Should().Be("Email");
        step.SelectorKey.Should().Be("collectForm.Email");
        step.Next.Should().Be("token");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenJsonWithoutNext_WhenCreate_ThenReturnsConfiguredStepWithoutNext()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserId",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email"
            }
            """);

        var factory = new GetUserIdStepFactory();

        // Act
        var step = (GetUserIdStep)factory.Create(json.RootElement, _sp);

        // Assert — at execution the identifier is written to "{kind}.UserId" (see GetUserIdStep.ExecuteAsync)
        step.SelectorField.Should().Be("Email");
        step.SelectorKey.Should().Be("collectForm.Email");
        step.Next.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenPhoneSelectorJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserId",
              "selectorField": "Phone",
              "selectorKey": "collectForm.Phone"
            }
            """);

        var factory = new GetUserIdStepFactory();

        // Act
        var step = (GetUserIdStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.SelectorField.Should().Be("Phone");
        step.SelectorKey.Should().Be("collectForm.Phone");
    }
}
