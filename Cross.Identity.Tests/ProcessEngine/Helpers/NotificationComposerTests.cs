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
}
