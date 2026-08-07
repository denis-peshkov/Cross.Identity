namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class Logout_StepFactoryTests
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
    [Category(TestCategory.UNIT)]
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "logout",
              "refreshTokenKey": "RefreshToken",
              "ipAddressKey": "collectForm.IpAddress",
              "next": "done"
            }
            """);

        var factory = new LogoutStepFactory();
        var step = (LogoutStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("logout");
        step.RefreshTokenKey.Should().Be("RefreshToken");
        step.IpAddressKey.Should().Be("collectForm.IpAddress");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
    }
}
