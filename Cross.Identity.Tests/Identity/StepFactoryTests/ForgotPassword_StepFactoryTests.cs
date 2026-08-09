namespace Cross.Identity.Tests.Identity.StepFactoryTests;

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
        sc.AddScoped<IUserService>(_ => Mock.Of<IUserService>());
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
              "kind": "forgotPassword",
              "channel": "email",
              "next": "collectResult"
            }
            """);

        var step = (ForgotPasswordStep)new ForgotPasswordStepFactory().Create(json.RootElement, _sp);

        step.Kind.Should().Be("forgotPassword");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.Next.Should().Be("collectResult");
        step.CodeService.Should().NotBeNull();
    }

}
