namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class LogoutAll_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IJwtTokenService>(_ => Mock.Of<IJwtTokenService>());
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
              "kind": "logoutAll",
              "refreshTokenKey": "RefreshToken",
              "ipAddressKey": "collectForm.IpAddress",
              "userAgentKey": "collectForm.UserAgent",
              "deviceFingerprint": "collectForm.DeviceFingerprint",
              "next": "done"
            }
            """);

        var factory = new LogoutAllStepFactory();
        var step = (LogoutAllStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("logoutAll");
        step.RefreshTokenKey.Should().Be("RefreshToken");
        step.IpAddressKey.Should().Be("collectForm.IpAddress");
        step.UserAgentKey.Should().Be("collectForm.UserAgent");
        step.DeviceFingerprintKey.Should().Be("collectForm.DeviceFingerprint");
        step.Next.Should().Be("done");
        step.JwtTokenService.Should().NotBeNull();
    }
}
