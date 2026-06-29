namespace Cross.Identity.Tests.Licensing;

[Category(TestCategory.UNIT)]
[TestFixture]
public sealed class LicenseAccessorTests
{
    [TearDown]
    public void TearDown()
    {
        LicenseCheckExtensions.ResetLicenseCheckForTests();
    }

    [Test]
    public void Current_WithoutLicenseKey_ShouldReturnUnconfiguredLicense()
    {
        var sut = new LicenseAccessor(
            new IdentityServiceConfiguration(),
            new LoggerFactory());

        var license = sut.Current;

        license.IsConfigured.Should().BeFalse();
        sut.Current.Should().BeSameAs(license);
    }

    [Test]
    public void Current_WithInvalidLicenseKey_ShouldReturnUnconfiguredLicense()
    {
        var sut = new LicenseAccessor(
            new IdentityServiceConfiguration { LicenseKey = "not-a-jwt" },
            new LoggerFactory());

        sut.Current.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void CheckLicense_ShouldRunOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new IdentityServiceConfiguration());
        services.AddSingleton<LicenseAccessor>();
        services.AddSingleton<LicenseValidator>();
        services.AddSingleton<ILicenseProductInfo, LicenseProductInfo>();

        var provider = services.BuildServiceProvider();

        provider.CheckLicense();
        provider.CheckLicense();

        provider.GetRequiredService<LicenseAccessor>().Current.IsConfigured.Should().BeFalse();
    }
}
