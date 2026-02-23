namespace Cross.Identity.UnitTests.Identity.StepFactoryTests;

[TestFixture]
public class PasswordAuth_StepFactoryTests
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
    public void PasswordAuthStepFactory_ShouldCreateStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "passwordAuth",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email",
              "passwordKey": "collectForm.Password",
              "userIdKey": "UserId",
              "next": "token"
            }
            """);

        var factory = new PasswordAuthStepFactory();

        // Act
        var step = (PasswordAuthStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("passwordAuth");
        step.SelectorField.Should().Be("Email");
        step.SelectorKey.Should().Be("collectForm.Email");
        step.PasswordKey.Should().Be("collectForm.Password");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("token");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void PasswordAuthStepFactory_ShouldUseDefaultUserIdKey()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "passwordAuth",
              "selectorField": "Email",
              "selectorKey": "collectForm.Email",
              "passwordKey": "collectForm.Password"
            }
            """);

        var factory = new PasswordAuthStepFactory();

        // Act
        var step = (PasswordAuthStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.UserIdKey.Should().Be("UserId"); // значение по умолчанию
    }

    [Test]
    public void PasswordAuthStepFactory_ShouldHandleUserNameSelector()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "passwordAuth",
              "selectorField": "UserName",
              "selectorKey": "collectForm.UserName",
              "passwordKey": "collectForm.Password"
            }
            """);

        var factory = new PasswordAuthStepFactory();

        // Act
        var step = (PasswordAuthStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.SelectorField.Should().Be("UserName");
    }
}
