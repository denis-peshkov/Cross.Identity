namespace Cross.Identity.Tests.Identity.StepFactoryTests;

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
        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidInitiateJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "externalLoginInitiate",
              "providerKey": "Provider",
              "returnUrlKey": "ReturnUrl",
              "userIdKey": "UserId",
              "refreshTokenKey": "RefreshToken",
              "next": "done"
            }
            """);

        var factory = new ExternalLoginInitiateStepFactory();
        var step = (ExternalLoginInitiateStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("externalLoginInitiate");
        step.ProviderKey.Should().Be("Provider");
        step.ReturnUrlKey.Should().Be("ReturnUrl");
        step.UserIdKey.Should().Be("UserId");
        step.RefreshTokenKey.Should().Be("RefreshToken");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidCompleteJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "externalLoginComplete",
              "codeKey": "Code",
              "stateKey": "State",
              "errorKey": "Error",
              "errorDescriptionKey": "ErrorDescription",
              "next": "done"
            }
            """);

        var factory = new ExternalLoginCompleteStepFactory();
        var step = (ExternalLoginCompleteStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("externalLoginComplete");
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
    [Category(TestCategory.UNIT)]
    public void GivenValidUnlinkJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "externalLoginUnlink",
              "providerKey": "Provider",
              "userIdKey": "UserId",
              "next": "done"
            }
            """);

        var factory = new ExternalLoginUnlinkStepFactory();
        var step = (ExternalLoginUnlinkStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("externalLoginUnlink");
        step.ProviderKey.Should().Be("Provider");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidGetAllJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "externalLoginGetAll",
              "userIdKey": "UserId",
              "next": "done"
            }
            """);

        var factory = new ExternalLoginGetAllStepFactory();
        var step = (ExternalLoginGetAllStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("externalLoginGetAll");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("done");
        step.ExternalLoginService.Should().NotBeNull();
    }
}
