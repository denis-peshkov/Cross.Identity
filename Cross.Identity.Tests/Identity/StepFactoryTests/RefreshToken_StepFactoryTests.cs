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
        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
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
              "jtiKey": "Jti",
              "next": "done"
            }
            """);

        var factory = new RefreshTokenStepFactory();
        var step = (RefreshTokenStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("refreshToken");
        step.JtiKey.Should().Be("Jti");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }
}
