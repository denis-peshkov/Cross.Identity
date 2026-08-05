namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ExternalLogin_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IExternalLoginService>(_ => Mock.Of<IExternalLoginService>());
        sc.AddSingleton<IJwtTokenService>(_ => Mock.Of<IJwtTokenService>());
        sc.AddSingleton<IUserService>(_ => Mock.Of<IUserService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void InitiateExternalLoginStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "initiateExternalLogin",
              "providerKey": "Provider",
              "returnUrlKey": "ReturnUrl",
              "linkUserIdKey": "LinkUserId",
              "next": "done"
            }
            """);

        var factory = new InitiateExternalLoginStepFactory();
        var step = (InitiateExternalLoginStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("initiateExternalLogin");
        step.ProviderKey.Should().Be("Provider");
        step.ReturnUrlKey.Should().Be("ReturnUrl");
        step.LinkUserIdKey.Should().Be("LinkUserId");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
    }

    [Test]
    public void CompleteExternalLoginStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "completeExternalLogin",
              "codeKey": "Code",
              "stateKey": "State",
              "errorKey": "Error",
              "errorDescriptionKey": "ErrorDescription",
              "next": "done"
            }
            """);

        var factory = new CompleteExternalLoginStepFactory();
        var step = (CompleteExternalLoginStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("completeExternalLogin");
        step.CodeKey.Should().Be("Code");
        step.StateKey.Should().Be("State");
        step.ErrorKey.Should().Be("Error");
        step.ErrorDescriptionKey.Should().Be("ErrorDescription");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
        step.JwtTokenService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void ExternalLoginUnlinkStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "externalLoginUnlink",
              "providerKey": "Provider",
              "next": "done"
            }
            """);

        var factory = new ExternalLoginUnlinkStepFactory();
        var step = (ExternalLoginUnlinkStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("externalLoginUnlink");
        step.ProviderKey.Should().Be("Provider");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
    }
}
