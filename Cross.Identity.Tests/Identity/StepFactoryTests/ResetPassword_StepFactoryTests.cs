namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
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
        sc.AddSingleton<IHttpContextAccessor>(_ => new HttpContextAccessor());
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "selectorKey": "forgotPassword.email",
              "passwordKey": "forgotPassword.password",
              "resolveBy": { "field": "Email" },
              "next": "done"
            }
            """);

        var factory = new ResetPasswordStepFactory();
        var step = (ResetPasswordStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("resetPassword");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.SelectorKey.Should().Be("forgotPassword.email");
        step.PasswordKey.Should().Be("forgotPassword.password");
        step.ResolveBy.Field.Should().Be("Email");
        step.Next.Should().Be("done");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void GivenMissingPasswordKey_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "selectorKey": "email",
              "resolveBy": { "field": "Email" }
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }

    [Test]
    public void GivenMissingChannel_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "selectorKey": "email",
              "passwordKey": "password",
              "resolveBy": { "field": "Email" }
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*channel*");
    }

    [Test]
    public void GivenMissingResolveBy_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "selectorKey": "email",
              "passwordKey": "password"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*resolveBy*");
    }
}
