namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ForgotPassword_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(_ => Mock.Of<ICodeService>());
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
        sc.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        sc.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "UnitTests",
            ContentRootPath = AppContext.BaseDirectory,
        });
        sc.AddScoped<IProcessDefinitionProvider>(_ => Mock.Of<IProcessDefinitionProvider>());
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
              "kind": "forgotPassword",
              "channel": "email",
              "selectorKey": "collectForm.Email",
              "resolveBy": { "field": "Email" },
              "next": "collectResult"
            }
            """);

        var step = (ForgotPasswordStep)new ForgotPasswordStepFactory().Create(json.RootElement, _sp);

        step.Kind.Should().Be("forgotPassword");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.SelectorKey.Should().Be("collectForm.Email");
        step.ResolveBy.Field.Should().Be("Email");
        step.Next.Should().Be("collectResult");
        step.CodeService.Should().NotBeNull();
    }

    [Test]
    public void GivenMissingResolveBy_WhenCreate_ThenThrowsInvalidOperationException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "forgotPassword",
              "channel": "email",
              "selectorKey": "collectForm.Email"
            }
            """);

        var act = () => new ForgotPasswordStepFactory().Create(json.RootElement, _sp);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*resolveBy*");
    }
}
