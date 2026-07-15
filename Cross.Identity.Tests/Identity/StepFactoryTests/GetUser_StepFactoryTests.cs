namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
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
    public void GetUserStepFactory_ShouldCreateStep()
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
    public void GetUserStepFactory_ShouldCreateStep_WithoutOptionalNext()
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
    public void GetUserStepFactory_ShouldHandlePhoneSelector()
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
