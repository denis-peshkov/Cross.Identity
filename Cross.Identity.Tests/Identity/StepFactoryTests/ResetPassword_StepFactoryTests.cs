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
        sc.AddSingleton<IProcessDefinitionProvider>(_ => Mock.Of<IProcessDefinitionProvider>());
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
              "passwordKey": "forgotPassword.password",
              "template": "password-changed",
              "subject": "Password changed",
              "next": "done"
            }
            """);

        var factory = new ResetPasswordStepFactory();
        var step = (ResetPasswordStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("resetPassword");
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.PasswordKey.Should().Be("forgotPassword.password");
        step.Template.Should().Be("password-changed");
        step.Subject.Should().Be("Password changed");
        step.Next.Should().Be("done");
        step.UserService.Should().NotBeNull();
        step.ProcessDefinitionProvider.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingPasswordKey_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "template": "password-changed",
              "subject": "Password changed"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingTemplate_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "passwordKey": "password",
              "subject": "Password changed"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingSubject_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "resetPassword",
              "passwordKey": "password",
              "template": "password-changed"
            }
            """);

        var factory = new ResetPasswordStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }
}
