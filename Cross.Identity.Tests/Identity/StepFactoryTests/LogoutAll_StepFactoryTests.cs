namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class LogoutAll_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IJwtTokenService>(_ => Mock.Of<IJwtTokenService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void LogoutAllStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "logoutAll",
              "refreshTokenKey": "RefreshToken",
              "next": "done"
            }
            """);

        var factory = new LogoutAllStepFactory();
        var step = (LogoutAllStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("logoutAll");
        step.RefreshTokenKey.Should().Be("RefreshToken");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
    }
}
