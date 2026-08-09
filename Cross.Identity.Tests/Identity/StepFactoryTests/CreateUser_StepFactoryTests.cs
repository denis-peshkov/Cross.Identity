namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class CreateUser_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IUserService>(p => Mock.Of<IUserService>());
        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidJsonWithMap_WhenCreate_ThenReturnsConfiguredStep()
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
        step.UserIdKey.Should().Be("UserId");
        step.Map.Should().HaveCount(3);
        step.Map["Email"].Should().Be("collectForm.Email");
        step.Map["Password"].Should().Be("collectForm.Password");
        step.Map["FullName"].Should().Be("collectForm.FullName");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenJsonWithoutUserIdKey_WhenCreate_ThenUsesDefaultUserIdKey()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "createUser",
              "map": {
                "Email": "collectForm.Email"
              }
            }
            """);

        var factory = new CreateUserStepFactory();

        // Act
        var step = (CreateUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.UserIdKey.Should().Be("UserId");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenRelativeKeysJson_WhenCreate_ThenReturnsConfiguredStep()
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
              "userIdKey": "UserId"
            }
            """);

        var factory = new CreateUserStepFactory();

        // Act
        var step = (CreateUserStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Map["Email"].Should().Be("Email");
        step.Map["Password"].Should().Be("Password");
    }
}
