namespace Cross.Identity.Extensions;

internal static class LicenseCheckExtensions
{
    private static bool _licenseChecked;

    internal static void CheckLicense(this IServiceProvider serviceProvider)
    {
        if (_licenseChecked)
        {
            return;
        }

        _licenseChecked = true;

        var licenseAccessor = serviceProvider.GetRequiredService<LicenseAccessor>();
        var licenseValidator = serviceProvider.GetRequiredService<LicenseValidator>();
        var license = licenseAccessor.Current;

        foreach (var licenseProductInfo in serviceProvider.GetServices<ILicenseProductInfo>())
        {
            licenseValidator.Validate(license, licenseProductInfo);
        }
    }

    internal static void ResetLicenseCheckForTests()
    {
        _licenseChecked = false;
    }
}
