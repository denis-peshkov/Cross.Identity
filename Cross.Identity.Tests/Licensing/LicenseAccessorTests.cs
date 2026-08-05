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
    public void GivenNoLicenseKey_WhenCurrentAccessed_ThenReturnsUnconfiguredLicense()
    {
        var sut = new LicenseAccessor(
            new IdentityServiceConfiguration(),
            new LoggerFactory());

        var license = sut.Current;

        license.IsConfigured.Should().BeFalse();
        sut.Current.Should().BeSameAs(license);
    }

    [Test]
    public void GivenInvalidLicenseKey_WhenCurrentAccessed_ThenReturnsUnconfiguredLicense()
    {
        var sut = new LicenseAccessor(
            new IdentityServiceConfiguration { LicenseKey = "not-a-jwt" },
            new LoggerFactory());

        sut.Current.IsConfigured.Should().BeFalse();
    }

    [Test]
    public void GivenServiceProvider_WhenCheckLicenseCalledTwice_ThenRunsOnlyOnce()
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
