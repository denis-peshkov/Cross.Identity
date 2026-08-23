namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
public class GetUserAccountId_StepFactoryTests
{
    private ServiceProvider _sp = null!;

    [SetUp]
    public void SetUp()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<IUserService>(p => Mock.Of<IUserService>());
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
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserAccountId",
              "next": "token"
            }
            """);

        var factory = new GetUserAccountIdStepFactory();

        // Act
        var step = (GetUserAccountIdStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Kind.Should().Be("getUserAccountId");
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.Next.Should().Be("token");
        step.UserService.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenJsonWithoutNext_WhenCreate_ThenReturnsConfiguredStepWithoutNext()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserAccountId"
            }
            """);

        var factory = new GetUserAccountIdStepFactory();

        // Act
        var step = (GetUserAccountIdStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
        step.Next.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMinimalJson_WhenCreate_ThenUsesDefaultSelectorKeys()
    {
        // Arrange
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "getUserAccountId"
            }
            """);

        var factory = new GetUserAccountIdStepFactory();

        // Act
        var step = (GetUserAccountIdStep)factory.Create(json.RootElement, _sp);

        // Assert
        step.Selector.FieldKey.Should().Be("collectForm.Field");
        step.Selector.ValueKey.Should().Be("collectForm.Value");
    }
}
