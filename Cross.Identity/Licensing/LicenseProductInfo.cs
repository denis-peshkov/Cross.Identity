namespace Cross.Identity.Licensing;

internal sealed class LicenseProductInfo : ILicenseProductInfo
{
    public string Company => "Peshkov software";

    public string Product => "Cross.Identity";

    public string Site => "https://peshkov.biz/identity";

    public ProductTypeEnum[] Types { get; } = { ProductTypeEnum.Cross_Identity };

    public string LicenseTypesErrMessage =>
        $"Your {Company} license does not include {Product} (expected {ProductTypeEnum.Cross_Identity} in the license type claim).";
}
