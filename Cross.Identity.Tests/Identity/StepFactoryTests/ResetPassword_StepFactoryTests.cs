namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class ResetPassword_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IUserService>(_ => Mock.Of<IUserService>());
        sc.AddScoped<IEmailSenderService>(_ => Mock.Of<IEmailSenderService>());
        sc.AddScoped<ISmsSenderService>(_ => Mock.Of<ISmsSenderService>());
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
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
              "kind": "resetPassword",
              "channel": "email",
              "passwordKey": "forgotPassword.password",
              "ipAddressKey": "collectForm.IpAddress",
              "userAgentKey": "collectForm.UserAgent",
              "deviceFingerprintKey": "collectForm.DeviceFingerprint",
              "next": "done"
            }
            """);

        var factory = new ResetPasswordStepFactory();
        var step = (ResetPasswordStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("resetPassword");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.PasswordKey.Should().Be("forgotPassword.password");
        step.IpAddressKey.Should().Be("collectForm.IpAddress");
        step.UserAgentKey.Should().Be("collectForm.UserAgent");
        step.Next.Should().Be("done");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingPasswordKey_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "ipAddressKey": "collectForm.IpAddress",
              "userAgentKey": "collectForm.UserAgent",
              "deviceFingerprintKey": "collectForm.DeviceFingerprint"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingChannel_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "passwordKey": "password",
              "ipAddressKey": "collectForm.IpAddress",
              "userAgentKey": "collectForm.UserAgent",
              "deviceFingerprintKey": "collectForm.DeviceFingerprint"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*channel*");
    }

}
