namespace Cross.Identity.UnitTests.Identity.StepFactoryTests;

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
              "kind": "getUser",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email",
              "userIdKey": "UserId",
              "next": "token"
            }
            """);

        var factory = new GetUserStepFactory();

        // Act
        var step = (GetUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("getUser");
        step.SelectorField.Should().Be("Email");
        step.SelectorKey.Should().Be("collectForm.Email");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("token");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void GetUserStepFactory_ShouldUseDefaultUserIdKey()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUser",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email"
            }
            """);

        var factory = new GetUserStepFactory();

        // Act
        var step = (GetUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.UserIdKey.Should().Be("UserId"); // значение по умолчанию
    }

    [Test]
    public void GetUserStepFactory_ShouldHandlePhoneSelector()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUser",
              "selectorField": "Phone",
              "selectorKey": "collectForm.Phone"
            }
            """);

        var factory = new GetUserStepFactory();

        // Act
        var step = (GetUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.SelectorField.Should().Be("Phone");
        step.SelectorKey.Should().Be("collectForm.Phone");
    }
}
