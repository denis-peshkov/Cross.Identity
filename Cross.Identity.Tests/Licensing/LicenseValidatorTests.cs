namespace Cross.Identity.Tests.Licensing;

[Category(TestCategory.UNIT)]
[TestFixture]
public sealed class LicenseValidatorTests
{
    private LicenseProductInfo _productInfo = null!;
    private LicenseValidator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _productInfo = new LicenseProductInfo();
        _sut = new LicenseValidator(new LoggerFactory());
    }

    [Test]
    public void GivenUnconfiguredLicense_WhenValidate_ThenDoesNotThrow()
    {
        var act = () => _sut.Validate(new License(), _productInfo);
        act.Should().NotThrow();
    }

    [Test]
    public void GivenExpiredLicense_WhenValidate_ThenDoesNotThrow()
    {
        var license = CreateConfiguredLicense(expiration: DateTimeOffset.UtcNow.AddDays(-1));

        var act = () => _sut.Validate(license, _productInfo);
        act.Should().NotThrow();
    }

    [Test]
    public void GivenWrongProductType_WhenValidate_ThenDoesNotThrow()
    {
        var license = CreateConfiguredLicense(productType: (ProductTypeEnum)999);

        var act = () => _sut.Validate(license, _productInfo);
        act.Should().NotThrow();
    }

    [Test]
    public void GivenValidLicense_WhenValidate_ThenDoesNotThrow()
    {
        var license = CreateConfiguredLicense();

        var act = () => _sut.Validate(license, _productInfo);
        act.Should().NotThrow();
    }

    private static License CreateConfiguredLicense(
        DateTimeOffset? expiration = null,
        ProductTypeEnum productType = ProductTypeEnum.Cross_Identity)
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("user_id", Guid.NewGuid().ToString()),
            new Claim("iat", now.ToUnixTimeSeconds().ToString()),
            new Claim("nbf", now.ToUnixTimeSeconds().ToString()),
            new Claim("exp", (expiration ?? now.AddYears(1)).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(EditionEnum.Standard)),
            new Claim("type", productType.ToString()),
        };

        return new License(new ClaimsPrincipal(new ClaimsIdentity(claims)));
    }
}
