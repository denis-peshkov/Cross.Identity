namespace Cross.Identity.Tests.Options;

[TestFixture]
public class NotificationOptionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void Defaults_MatchLegacyHardcodedBrand()
    {
        var opt = new NotificationOptions();
        opt.Brand.Should().Be("peshkov.biz");
        opt.Site.Should().Be("peshkov.biz");
        opt.Company.Should().Be("Peshkov");
        opt.FullName.Should().Be("Denis Peshkov");
        opt.SupportEmail.Should().Be("support@peshkov.biz");
    }
}
