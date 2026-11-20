namespace Cross.Identity.Tests.Identity.StepFactoryTests;

[TestFixture]
[Category(TestCategory.UNIT)]
public class CommunicationEndpointAdd_StepFactoryTests
{
    [Test]
    public void AddFactory_CreatesStepWithConfiguredKeys()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        sc.AddScoped<ICodeService>(_ => Mock.Of<ICodeService>());
        sc.AddScoped<INotificationComposer>(_ => Mock.Of<INotificationComposer>());
        sc.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        sc.AddLogging();
        using var sp = sc.BuildServiceProvider();

        var cfg = JsonDocument.Parse(
            """
            {
              "userAccountIdKey": "collectForm.UserAccountId",
              "emailKey": "collectForm.Email",
              "phoneKey": "collectForm.PhoneNumber",
              "ttlKey": "collectForm.Ttl",
              "template": "verify",
              "subject": "Verification Code",
              "next": "collectResult"
            }
            """).RootElement;

        var factory = new CommunicationEndpointAddStepFactory();
        factory.Kind.Should().Be("communicationEndpointAdd");

        var step = factory.Create(cfg, sp).Should().BeOfType<CommunicationEndpointAddStep>().Subject;
        step.UserAccountIdKey.Should().Be("collectForm.UserAccountId");
        step.EmailKey.Should().Be("collectForm.Email");
        step.PhoneKey.Should().Be("collectForm.PhoneNumber");
        step.TtlKey.Should().Be("collectForm.Ttl");
        step.Template.Should().Be("verify");
        step.Subject.Should().Be("Verification Code");
        step.Next.Should().Be("collectResult");
    }

    [Test]
    public void SetVerifiedFactory_CreatesStepWithConfiguredKeys()
    {
        var sc = new ServiceCollection();
        sc.AddScoped<ICommunicationEndpointService>(_ => Mock.Of<ICommunicationEndpointService>());
        sc.AddScoped<ICodeService>(_ => Mock.Of<ICodeService>());
        sc.AddLogging();
        using var sp = sc.BuildServiceProvider();

        var cfg = JsonDocument.Parse(
            """
            {
              "userAccountIdKey": "collectForm.UserAccountId",
              "emailKey": "collectForm.Email",
              "phoneKey": "collectForm.PhoneNumber",
              "codeKey": "collectForm.Code",
              "next": "collectResult"
            }
            """).RootElement;

        var factory = new CommunicationEndpointSetVerifiedStepFactory();
        factory.Kind.Should().Be("communicationEndpointSetVerified");

        var step = factory.Create(cfg, sp).Should().BeOfType<CommunicationEndpointSetVerifiedStep>().Subject;
        step.UserAccountIdKey.Should().Be("collectForm.UserAccountId");
        step.EmailKey.Should().Be("collectForm.Email");
        step.PhoneKey.Should().Be("collectForm.PhoneNumber");
        step.CodeKey.Should().Be("collectForm.Code");
        step.Next.Should().Be("collectResult");
    }
}
