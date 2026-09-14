namespace Cross.Identity.Tests.ProcessEngine.Helpers;

[TestFixture]
public class NotificationComposerTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void Compose_AppliesBrandAndPlaceholders()
    {
        var provider = new Mock<IProcessDefinitionProvider>();
        provider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("{{company}} {{code}} {{supportEmail}}");
        provider.Setup(p => p.GetTemplate("verify", "en", "html"))
            .Returns("<b>{{code}}</b>");

        var sut = new NotificationComposer(
            provider.Object,
            Microsoft.Extensions.Options.Options.Create(new NotificationOptions
            {
                Company = "Acme",
                SupportEmail = "help@acme.test",
            }));

        var bag = new Bag();
        var bodies = sut.Compose(
            bag,
            "verify",
            new Dictionary<string, string> { ["{{code}}"] = "123456" });

        bodies.TextBody.Should().Be("Acme 123456 help@acme.test");
        bodies.HtmlBody.Should().Be("<b>123456</b>");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void Compose_WhenLanguageRu_UsesRuTemplate()
    {
        var provider = new Mock<IProcessDefinitionProvider>();
        provider.Setup(p => p.GetTemplate("verify", "ru", "txt")).Returns("Код {{code}}");
        provider.Setup(p => p.GetTemplate("verify", "ru", "html")).Returns("<p>{{code}}</p>");

        var sut = new NotificationComposer(
            provider.Object,
            Microsoft.Extensions.Options.Options.Create(new NotificationOptions()));

        var bag = new Bag().Set("collectForm.LanguageCode", "ru");
        var bodies = sut.Compose(
            bag,
            "verify",
            new Dictionary<string, string> { ["{{code}}"] = "99" });

        bodies.TextBody.Should().Be("Код 99");
        provider.Verify(p => p.GetTemplate("verify", "en", "txt"), Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmbeddedVerifyHtml_WhenCompose_ThenCodeHasNoLeftoverBraces()
    {
        var provider = new EmbeddedResourceProcessDefinitionProvider(
            Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
            {
                Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
                BaseNamespace = "Cross.Identity.ProcessEngine.Definitions",
            }));

        var sut = new NotificationComposer(
            provider,
            Microsoft.Extensions.Options.Options.Create(new NotificationOptions
            {
                Brand = "BrandX",
                Company = "Acme",
                FullName = "Ada",
                Site = "https://example.test",
                SupportEmail = "help@acme.test",
            }));

        var bodies = sut.Compose(
            new Bag(),
            "verify",
            new Dictionary<string, string>
            {
                ["{{code}}"] = "123456",
                ["{{expires}}"] = "15m",
                ["{{verificationLink}}"] = "https://example.test/v",
                ["{{helpLink}}"] = "https://example.test/help",
            });

        bodies.HtmlBody.Should().Contain("123456");
        bodies.HtmlBody.Should().NotContain("{{123456}}");
        bodies.HtmlBody.Should().NotContain("{{{{");
        bodies.HtmlBody.Should().Contain("Your code: 123456.");
    }
}
