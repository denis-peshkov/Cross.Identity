namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class RefreshToken_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
        sc.AddScoped<IJwtTokenService>(_ => Mock.Of<IJwtTokenService>());
        sc.AddScoped<IUserService>(_ => Mock.Of<IUserService>());
        sc.AddSingleton<IOptionsSnapshot<AuthenticationOptions>>(_ =>
        {
            var mock = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
            mock.Setup(m => m.Value).Returns(new AuthenticationOptions());
            return mock.Object;
        });
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
              "kind": "refreshToken",
              "refreshTokenKey": "RefreshToken",
              "ipAddressKey": "collectForm.IpAddress",
              "userAgentKey": "collectForm.UserAgent",
              "next": "done"
            }
            """);

        var factory = new RefreshTokenStepFactory();
        var step = (RefreshTokenStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("refreshToken");
        step.RefreshTokenKey.Should().Be("RefreshToken");
        step.IpAddressKey.Should().Be("collectForm.IpAddress");
        step.UserAgentKey.Should().Be("collectForm.UserAgent");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }
}
