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
        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmailChannelJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "template": "verify",
              "subject": "Verification Code",
              "next": "verifyCode"
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("sendCode");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.TtlKey.Should().BeNull();
        step.Template.Should().Be("verify");
        step.Subject.Should().Be("Verification Code");
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
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "sms",
              "template": "verify",
              "subject": "Verification Code"
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

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
              "template": "verify",
              "subject": "Verification Code",
              "ttlKey": "collectForm.Ttl"
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        step.TtlKey.Should().Be("collectForm.Ttl");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingChannel_WhenCreate_ThenThrowsKeyNotFoundException()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "template": "verify",
              "subject": "Verification Code"
            }
            """);

        var factory = new SendCodeStepFactory();

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
              "kind": "sendCode",
              "channel": "email",
              "subject": "Verification Code"
            }
            """);

        var factory = new SendCodeStepFactory();

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
              "kind": "sendCode",
              "channel": "email",
              "template": "verify"
            }
            """);

        var factory = new SendCodeStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<KeyNotFoundException>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenResetOptionsInJson_WhenCreate_ThenBindsTemplateAndSubject()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "sendCode",
              "channel": "email",
              "template": "reset",
              "subject": "Reset your password"
            }
            """);

        var factory = new SendCodeStepFactory();
        var step = (SendCodeStep)factory.Create(json.RootElement, _sp);

        step.Template.Should().Be("reset");
        step.Subject.Should().Be("Reset your password");
    }
}
