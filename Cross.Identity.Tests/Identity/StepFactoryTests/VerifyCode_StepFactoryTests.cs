namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class VerifyCode_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(p => Mock.Of<ICodeService>());
        sc.AddScoped<IUserService>(_ => Mock.Of<IUserService>());
        sc.AddSingleton<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        sc.AddSingleton<ILoggerFactory>(_ => new LoggerFactory());
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
              "kind": "verifyCode",
              "channel": "email",
              "codeKey": "collectForm.Code",
              "next": "token"
            }
            """);

        var factory = new VerifyCodeStepFactory();
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("verifyCode");
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.CodeKey.Should().Be("collectForm.Code");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("token");
        step.CodeService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSmsChannelJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "sms",
              "codeKey": "collectForm.Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenUserIdKeyInJson_WhenCreate_ThenBindsUserIdKey()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "email",
              "codeKey": "Code",
              "userIdKey": "Id"
            }
            """);

        var factory = new VerifyCodeStepFactory();
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        step.CodeKey.Should().Be("Code");
        step.UserIdKey.Should().Be("Id");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMissingChannel_WhenCreate_ThenStillCreatesStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "codeKey": "collectForm.Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        step.CodeKey.Should().Be("collectForm.Code");
        step.CommunicationEndpoints.Should().NotBeNull();
    }
}
