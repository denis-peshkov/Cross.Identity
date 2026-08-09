namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class SendCode_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(p => Mock.Of<ICodeService>());
        sc.AddScoped<IUserService>(p => Mock.Of<IUserService>());
        sc.AddSingleton<ILoggerFactory>(p => new LoggerFactory());
        sc.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:ClientUrl"] = "http://localhost:4200"
            })
            .Build());
        var env = new HostingEnvironment
        {
            EnvironmentName = Environments.Development,
            ApplicationName = "UnitTests",
            ContentRootPath = AppContext.BaseDirectory
        };
        sc.AddSingleton<IHostEnvironment>(env);
        sc.AddScoped<IProcessDefinitionProvider>(p => Mock.Of<IProcessDefinitionProvider>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmailChannelJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "next": "verifyCode"
            }
            """);

        var factory = new SendCodeStepFactory();

        // Act
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("sendCode");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.TtlKey.Should().BeNull();
        step.Next.Should().Be("verifyCode");
        step.CodeService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
        step.Environment.Should().NotBeNull();
        step.ProcessDefinitionProvider.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSmsChannelJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "sms"
            }
            """);

        var factory = new SendCodeStepFactory();

        // Act
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Channel.Should().Be(ChannelEnum.Sms);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenTtlKeyInJson_WhenCreate_ThenBindsTtlKey()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "ttlKey": "collectForm.Ttl"
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        step.TtlKey.Should().Be("collectForm.Ttl");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingChannel_WhenCreate_ThenThrowsInvalidOperationException()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode"
            }
            """);

        var factory = new SendCodeStepFactory();

        // Act & Assert
        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*'channel' is required*");
    }
}
