namespace Cross.Identity.Tests.ProcessEngine.Core;

[TestFixture]
public class HostSuppliedLanguageContextTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void Read_WhenMissing_ReturnsEmpty()
    {
        var bag = new Bag();
        var ctx = HostSuppliedLanguageContext.Read(bag);
        ctx.IsEmpty.Should().BeTrue();
        ctx.ResolveLanguageCode().Should().Be("en");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void Read_WhenPresent_NormalizesToLower()
    {
        var bag = new Bag();
        bag.Set("collectForm.LanguageCode", "RU");
        HostSuppliedLanguageContext.Read(bag).ResolveLanguageCode().Should().Be("ru");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void ResolveLanguageCode_WhenInvalid_FallsBackToEn()
    {
        new HostSuppliedLanguageContext("r").ResolveLanguageCode().Should().Be("en");
        new HostSuppliedLanguageContext("rus").ResolveLanguageCode().Should().Be("en");
        new HostSuppliedLanguageContext("12").ResolveLanguageCode().Should().Be("en");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void ResolveTemplate_WhenLanguageMissing_FallsBackToEn()
    {
        var provider = new Mock<IProcessDefinitionProvider>();
        provider.Setup(p => p.GetTemplate("verify", "ru", "txt"))
            .Throws(new KeyNotFoundException());
        provider.Setup(p => p.GetTemplate("verify", "en", "txt"))
            .Returns("EN body");

        var body = new HostSuppliedLanguageContext("ru")
            .ResolveTemplate(provider.Object, "verify", "txt");

        body.Should().Be("EN body");
        provider.Verify(p => p.GetTemplate("verify", "ru", "txt"), Times.Once);
        provider.Verify(p => p.GetTemplate("verify", "en", "txt"), Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void ResolveTemplate_WhenLanguagePresent_UsesIt()
    {
        var provider = new Mock<IProcessDefinitionProvider>();
        provider.Setup(p => p.GetTemplate("verify", "ru", "txt")).Returns("RU body");

        var body = new HostSuppliedLanguageContext("ru")
            .ResolveTemplate(provider.Object, "verify", "txt");

        body.Should().Be("RU body");
        provider.Verify(p => p.GetTemplate("verify", "en", "txt"), Times.Never);
    }
}
