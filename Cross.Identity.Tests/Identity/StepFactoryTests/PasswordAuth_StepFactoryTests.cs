namespace Cross.Identity.Tests.Identity.StepFactoryTests;

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
    [Category(TestCategory.UNIT)]
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
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
    [Category(TestCategory.UNIT)]
    public void GivenJsonWithoutUserIdKey_WhenCreate_ThenUsesDefaultUserIdKey()
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
        step.UserIdKey.Should().Be("UserId"); // default value
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenUserNameSelectorJson_WhenCreate_ThenReturnsConfiguredStep()
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
