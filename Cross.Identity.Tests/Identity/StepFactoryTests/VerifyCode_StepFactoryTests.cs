namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
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
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
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
    public void GivenPhoneChannelJson_WhenCreate_ThenReturnsConfiguredStep()
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
    public void GivenRelativeKeysJson_WhenCreate_ThenReturnsConfiguredStep()
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
