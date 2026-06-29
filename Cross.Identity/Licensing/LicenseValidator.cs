namespace Cross.Identity.Licensing;

internal sealed class LicenseValidator
{
    private readonly ILogger _logger;

    public LicenseValidator(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("Peshkov.Cross.Identity.License");
    }

    /// <summary>
    /// Validates license using only data from the key (JWT claims), including <see cref="License.ProductType"/>.
    /// </summary>
    public void Validate(License license, ILicenseProductInfo licenseProductInfo)
    {
        _logger.LogDebug("The {Company} license key details: {@License}", licenseProductInfo.Company, license);

        var errors = new List<string>();

        if (license is not { IsConfigured: true })
        {
            var message =
                $"You do not have a valid license key for the {licenseProductInfo.Company} {licenseProductInfo.Product}. " +
                "This is allowed for development and testing scenarios. " +
                "If you are running in production you are required to have a licensed version. " +
                $"Please visit {licenseProductInfo.Site} to obtain a valid license.";

            _logger.LogCritical(message);
            return;
        }

        var diff = DateTime.UtcNow.Date.Subtract(license.ExpirationDate!.Value.Date).TotalDays;
        if (diff > 0)
        {
            errors.Add($"Your license for the {licenseProductInfo.Company} {licenseProductInfo.Product} expired {diff} days ago.");
        }

        if (licenseProductInfo.Types.All(x => x != license.ProductType!.Value))
        {
            errors.Add(licenseProductInfo.LicenseTypesErrMessage);
        }

        if (errors.Count > 0)
        {
            foreach (var err in errors)
            {
                _logger.LogError(err);
            }

            _logger.LogCritical(
                "Please visit {Site} to obtain a valid license for the {Company} {Product}.",
                licenseProductInfo.Site,
                licenseProductInfo.Company,
                licenseProductInfo.Product);
        }
        else
        {
            _logger.LogInformation(
                "You have a valid license key for the {Company} {Type} {Edition} edition. The license expires on {LicenseExpiration}.",
                licenseProductInfo.Company,
                license.ProductType,
                license.Edition,
                license.ExpirationDate);
        }
    }
}
