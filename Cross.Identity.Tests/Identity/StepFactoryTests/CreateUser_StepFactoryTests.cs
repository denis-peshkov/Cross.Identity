namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class CreateUser_StepFactoryTests
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
    public void CreateUserStepFactory_ShouldCreateStepWithMap()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "createUser",
              "map": {
                "Email": "collectForm.Email",
                "Password": "collectForm.Password",
                "FullName": "collectForm.FullName"
              },
              "selectorKey": "collectForm.Email",
              "userIdKey": "UserId",
              "next": "sendCode"
            }
            """);

        var factory = new CreateUserStepFactory();

        // Act
        var step = (CreateUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("createUser");
        step.Next.Should().Be("sendCode");
        step.SelectorKey.Should().Be("collectForm.Email");
        step.UserIdKey.Should().Be("UserId");
        step.Map.Should().HaveCount(3);
        step.Map["Email"].Should().Be("collectForm.Email");
        step.Map["Password"].Should().Be("collectForm.Password");
        step.Map["FullName"].Should().Be("collectForm.FullName");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void CreateUserStepFactory_ShouldUseDefaultUserIdKey()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "createUser",
              "map": {
                "Email": "collectForm.Email"
              },
              "selectorKey": "collectForm.Email"
            }
            """);

        var factory = new CreateUserStepFactory();

        // Act
        var step = (CreateUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.UserIdKey.Should().Be("UserId"); // значение по умолчанию
    }

    [Test]
    public void CreateUserStepFactory_ShouldHandleRelativeKeys()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "createUser",
              "map": {
                "Email": "Email",
                "Password": "Password"
              },
              "selectorKey": "Email",
              "userIdKey": "UserId"
            }
            """);

        var factory = new CreateUserStepFactory();

        // Act
        var step = (CreateUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Map["Email"].Should().Be("Email");
        step.Map["Password"].Should().Be("Password");
        step.SelectorKey.Should().Be("Email");
    }
}
