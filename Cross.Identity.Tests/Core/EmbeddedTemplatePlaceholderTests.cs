namespace Cross.Identity.Tests.Core;

[TestFixture]
public sealed class EmbeddedTemplatePlaceholderTests
{
    private static readonly string[] TemplateNames =
    {
        "verify", "reset", "confirm-email", "register", "password-changed",
    };

    private static readonly string[] Languages = { "en", "ru", "ro" };

    private static EmbeddedResourceProcessDefinitionProvider CreateSut() =>
        new(Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions"
        }));

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("verify", "en")]
    [TestCase("verify", "ru")]
    [TestCase("verify", "ro")]
    public void VerifyTemplates_ContainRequiredPlaceholders(string name, string lang)
    {
        var sut = CreateSut();
        foreach (var format in new[] { "txt", "html" })
        {
            var body = sut.GetTemplate(name, lang, format);
            body.Should().Contain("{{company}}");
            body.Should().Contain("{{fullName}}");
            body.Should().Contain("{{code}}");
            body.Should().Contain("{{verificationLink}}");
            body.Should().Contain("{{expires}}");
            body.Should().Contain("{{supportEmail}}");
            body.Should().Contain("{{site}}");
            body.Should().Contain("{{helpLink}}");
            body.Should().NotContain("{{{{");
            body.Should().NotContain("Bioclinica");
            body.Should().NotContain("958748");
        }
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("reset", "en")]
    [TestCase("reset", "ru")]
    [TestCase("reset", "ro")]
    [TestCase("confirm-email", "en")]
    [TestCase("confirm-email", "ru")]
    [TestCase("confirm-email", "ro")]
    public void OtpTemplates_ContainRequiredPlaceholders(string name, string lang)
    {
        var sut = CreateSut();
        foreach (var format in new[] { "txt", "html" })
        {
            var body = sut.GetTemplate(name, lang, format);
            body.Should().Contain("{{code}}");
            body.Should().Contain("{{email}}");
            body.Should().Contain("{{expires}}");
            body.Should().Contain("{{url}}");
            body.Should().Contain("{{supportEmail}}");
            body.Should().Contain("{{brand}}");
            body.Should().Contain("{{year}}");
            body.Should().NotContain("{{{{");
            body.Should().NotContain("Bioclinica");
        }
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("en")]
    [TestCase("ru")]
    [TestCase("ro")]
    public void PasswordChangedTemplates_ContainRequiredPlaceholders(string lang)
    {
        var sut = CreateSut();
        foreach (var format in new[] { "txt", "html" })
        {
            var body = sut.GetTemplate("password-changed", lang, format);
            body.Should().Contain("{{changedAt}}");
            body.Should().Contain("{{ip}}");
            body.Should().Contain("{{supportEmail}}");
            body.Should().Contain("{{brand}}");
            body.Should().Contain("{{year}}");
            body.Should().NotContain("{{{{");
        }
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("en")]
    [TestCase("ru")]
    [TestCase("ro")]
    public void RegisterTemplates_ContainRequiredPlaceholders(string lang)
    {
        var sut = CreateSut();
        foreach (var format in new[] { "txt", "html" })
        {
            var body = sut.GetTemplate("register", lang, format);
            body.Should().Contain("{{fullName}}");
            body.Should().Contain("{{email}}");
            body.Should().Contain("{{company}}");
            body.Should().Contain("{{url}}");
            body.Should().Contain("{{supportEmail}}");
            body.Should().Contain("{{brand}}");
            body.Should().Contain("{{year}}");
            body.Should().NotContain("{{{{");
            body.Should().NotContain("Bioclinica");
        }
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAllEmbeddedTemplates_WhenLoaded_ThenHaveNoQuadrupleBracePlaceholders()
    {
        var sut = CreateSut();
        foreach (var name in TemplateNames)
        {
            foreach (var lang in Languages)
            {
                foreach (var format in new[] { "txt", "html" })
                {
                    var body = sut.GetTemplate(name, lang, format);
                    body.Should().NotContain(
                        "{{{{",
                        because: $"{name}.{lang}.{format} must use double-brace placeholders for NotificationComposer");
                }
            }
        }
    }
}
