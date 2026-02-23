namespace Cross.Identity.UnitTests.Identity.StepFactoryTests;

[TestFixture]
public class VerifyCode_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(p => Mock.Of<ICodeService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void VerifyCodeStepFactory_ShouldCreateStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "email",
              "identityKey": "collectForm.Email",
              "codeKey": "collectForm.Code",
              "next": "token"
            }
            """);

        var factory = new VerifyCodeStepFactory();

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("verifyCode");
        step.Channel.Should().Be("email");
        step.IdentityKey.Should().Be("collectForm.Email");
        step.CodeKey.Should().Be("collectForm.Code");
        step.Next.Should().Be("token");
        step.CodeService.Should().NotBeNull();
    }

    [Test]
    public void VerifyCodeStepFactory_ShouldHandlePhoneChannel()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "phone",
              "identityKey": "collectForm.Phone",
              "codeKey": "collectForm.Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Channel.Should().Be("phone");
    }

    [Test]
    public void VerifyCodeStepFactory_ShouldHandleRelativeKeys()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "email",
              "identityKey": "Email",
              "codeKey": "Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.IdentityKey.Should().Be("Email");
        step.CodeKey.Should().Be("Code");
    }
}
