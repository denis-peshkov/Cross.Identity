namespace Cross.Identity.UnitTests.Identity.StepFactoryTests;

[TestFixture]
public class ResetPassword_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(_ => Mock.Of<ICodeService>());
        sc.AddScoped<IUserService>(_ => Mock.Of<IUserService>());
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
        sc.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = "Test" });
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void ResetPasswordStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "selectorKey": "forgotPassword.email",
              "resolveBy": { "field": "Email" },
              "next": "done"
            }
            """);

        var factory = new ResetPasswordStepFactory();
        var step = (ResetPasswordStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("resetPassword");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.SelectorKey.Should().Be("forgotPassword.email");
        step.ResolveBy.Field.Should().Be("Email");
        step.Next.Should().Be("done");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void ResetPasswordStepFactory_ShouldThrowWhenChannelMissing()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "selectorKey": "email",
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
    public void ResetPasswordStepFactory_ShouldThrowWhenResolveByMissing()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "channel": "email",
              "selectorKey": "email"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*resolveBy*");
    }
}
