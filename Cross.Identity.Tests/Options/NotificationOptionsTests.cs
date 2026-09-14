namespace Cross.Identity.Tests.Options;

[TestFixture]
public class NotificationOptionsTests
{
    /// <summary>
    /// Brand placeholders are host-configured via <c>Authentication:Notifications</c>
    /// (see Sample.Api <c>appsettings.json</c>). The options type itself has no built-in defaults.
    /// </summary>
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNewOptions_WhenConstructed_ThenHasNoBuiltInBrandDefaults()
    {
        var opt = new NotificationOptions();
        opt.Brand.Should().BeNull();
        opt.Site.Should().BeNull();
        opt.Company.Should().BeNull();
        opt.FullName.Should().BeNull();
        opt.SupportEmail.Should().BeNull();
    }
}
