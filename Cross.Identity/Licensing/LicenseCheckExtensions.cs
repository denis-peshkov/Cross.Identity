namespace Cross.Identity.Licensing;

internal static class LicenseCheckExtensions
{
    private static bool _licenseChecked;

    internal static void CheckLicense(this IServiceProvider serviceProvider)
    {
        if (!_licenseChecked)
        {
            var licenseAccessor = serviceProvider.GetRequiredService<LicenseAccessor>();
            var licenseValidator = serviceProvider.GetRequiredService<LicenseValidator>();
            var license = licenseAccessor.Current;

            foreach (var licenseProductInfo in serviceProvider.GetServices<ILicenseProductInfo>())
            {
                if (licenseProductInfo.Product == "Cross.Identity")
                {
                    licenseValidator.Validate(license, licenseProductInfo);
                }
            }
        }

        // if True then check will be performed only once
        _licenseChecked = false;
    }

    internal static void ResetLicenseCheckForTests() => _licenseChecked = false;
}
