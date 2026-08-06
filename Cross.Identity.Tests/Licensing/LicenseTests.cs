namespace Cross.Identity.Tests.Licensing;

[TestFixture]
public sealed class LicenseTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenParameterlessConstructor_WhenCreated_ThenIsNotConfigured()
    {
        var sut = new License();

        sut.IsConfigured.Should().BeFalse();
        sut.UserId.Should().BeNull();
        sut.ProductType.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAllRequiredClaims_WhenCreated_ThenIsConfigured()
    {
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new Claim("sub_id", Guid.NewGuid().ToString()),
            new Claim("user_id", Guid.NewGuid().ToString()),
            new Claim("iat", now.ToUnixTimeSeconds().ToString()),
            new Claim("nbf", now.ToUnixTimeSeconds().ToString()),
            new Claim("exp", now.AddYears(1).ToUnixTimeSeconds().ToString()),
            new Claim("edition", nameof(EditionEnum.Professional)),
            new Claim("type", nameof(ProductTypeEnum.Cross_Identity)),
        };

        var sut = new License(new ClaimsPrincipal(new ClaimsIdentity(claims)));

        sut.IsConfigured.Should().BeTrue();
        sut.Edition.Should().Be(EditionEnum.Professional);
        sut.ProductType.Should().Be(ProductTypeEnum.Cross_Identity);
    }
}
