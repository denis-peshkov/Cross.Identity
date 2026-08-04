namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[Category(TestCategory.UNIT)]
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
    public void SendCodeStepFactory_ShouldCreateStepWithEmailChannel()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "selectorKey": "collectForm.Email",
              "resolveBy": {
                "field": "Email"
              },
              "next": "verifyCode"
            }
            """);

        var factory = new SendCodeStepFactory();

        // Act
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("sendCode");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.SelectorKey.Should().Be("collectForm.Email");
        step.ResolveBy.Field.Should().Be("Email");
        step.TtlKey.Should().BeNull();
        step.Next.Should().Be("verifyCode");
        step.CodeService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
        step.Environment.Should().NotBeNull();
        step.ProcessDefinitionProvider.Should().NotBeNull();
    }

    [Test]
    public void SendCodeStepFactory_ShouldCreateStepWithSmsChannel()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "sms",
              "selectorKey": "collectForm.Phone",
              "resolveBy": {
                "field": "Phone"
              }
            }
            """);

        var factory = new SendCodeStepFactory();

        // Act
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Channel.Should().Be(ChannelEnum.Sms);
        step.ResolveBy.Field.Should().Be("Phone");
    }

    [Test]
    public void SendCodeStepFactory_ShouldBindTtlKey()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "selectorKey": "collectForm.Email",
              "ttlKey": "collectForm.Ttl",
              "resolveBy": {
                "field": "Email"
              }
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        step.TtlKey.Should().Be("collectForm.Ttl");
    }

    [Test]
    public void SendCodeStepFactory_ShouldThrowWhenChannelMissing()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "selectorKey": "collectForm.Email"
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
