namespace Cross.Identity.UnitTests.Identity.StepFactoryTests;

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
        _sp = sc.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => _sp.Dispose();

    [Test]
    public void CodeAuthStepFactory_ShouldCreateStep()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "codeAuth",
              "channel": "email",
              "identityKey": "auth-form.Email",
              "codeKey": "auth-form.Code",
              "resolveBy": { "field": "Email" },
              "userIdKey": "UserId",
              "next": "token"
            }
            """);

        var factory = new CodeAuthStepFactory();
        var step = (CodeAuthStep)factory.Create(json.RootElement, _sp);

        step.Kind.Should().Be("codeAuth");
        step.Channel.Should().Be("email");
        step.IdentityKey.Should().Be("auth-form.Email");
        step.CodeKey.Should().Be("auth-form.Code");
        step.ResolveBy.Field.Should().Be("Email");
        step.UserIdKey.Should().Be("UserId");
        step.Next.Should().Be("token");
        step.CodeService.Should().NotBeNull();
        step.UserService.Should().NotBeNull();
    }

    [Test]
    public void CodeAuthStepFactory_ShouldUseDefaultUserIdKey()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "codeAuth",
              "channel": "phone",
              "identityKey": "auth-form.Phone",
              "codeKey": "auth-form.Code",
              "resolveBy": { "field": "Phone" }
            }
            """);

        var factory = new CodeAuthStepFactory();
        var step = (CodeAuthStep)factory.Create(json.RootElement, _sp);

        step.UserIdKey.Should().Be("UserId");
    }

    [Test]
    public void CodeAuthStepFactory_ShouldThrowWhenResolveByMissing()
    {
        using var json = JsonDocument.Parse(
            """
            {
              "kind": "codeAuth",
              "channel": "email",
              "identityKey": "auth-form.Email",
              "codeKey": "auth-form.Code"
            }
            """);

        var factory = new CodeAuthStepFactory();

        FluentActions.Invoking(() => factory.Create(json.RootElement, _sp))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*resolveBy*");
    }
}
