namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class VerifyToken_StepFactoryTests
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
              "kind": "verifyToken",
              "accessTokenKey": "AccessToken",
              "next": "done"
            }
            """);

        var factory = new VerifyTokenStepFactory();
        var step = (VerifyTokenStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("verifyToken");
        step.AccessTokenKey.Should().Be("AccessToken");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
    }
}
