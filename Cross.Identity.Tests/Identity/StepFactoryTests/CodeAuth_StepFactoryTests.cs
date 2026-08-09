namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class CodeAuth_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICodeService>(_ => Mock.Of<ICodeService>());
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
              "kind": "codeAuth",
              "channel": "email",
              "codeKey": "auth-form.Code",
              "userIdKey": "UserId",
              "next": "token"
            }
            """);

        var factory = new CodeAuthStepFactory();
        var step = (CodeAuthStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("codeAuth");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.CodeKey.Should().Be("auth-form.Code");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("token");
        step.CodeService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenJsonWithoutUserIdKey_WhenCreate_ThenUsesDefaultUserIdKey()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "codeAuth",
              "channel": "sms",
              "codeKey": "auth-form.Code"
            }
            """);

        var factory = new CodeAuthStepFactory();
        var step = (CodeAuthStep)factory.Create(json.RootElement, _sp);

        step.UserIdKey.Should().Be("UserId");
        step.Channel.Should().Be(ChannelEnum.Sms);
    }

}
