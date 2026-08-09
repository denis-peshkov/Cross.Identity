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
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
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

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("verifyCode");
        step.Channel.Should().Be(ChannelEnum.Email);
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.CodeKey.Should().Be("collectForm.Code");
        step.Next.Should().Be("token");
        step.CodeService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSmsChannelJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "sms",
              "codeKey": "collectForm.Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Channel.Should().Be(ChannelEnum.Sms);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenRelativeCodeKeyJson_WhenCreate_ThenReturnsConfiguredStep()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "verifyCode",
              "channel": "email",
              "codeKey": "Code"
            }
            """);

        var factory = new VerifyCodeStepFactory();

        // Act
        var step = (VerifyCodeStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.CodeKey.Should().Be("Code");
        step.Selector.FieldKey.Should().Be("collectForm.Field");
    }
}
