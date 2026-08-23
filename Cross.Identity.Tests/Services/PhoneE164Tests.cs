namespace Cross.Identity.Tests.Services;

[TestFixture]
public class PhoneE164Tests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidPhoneNumber_WhenNormalize_ThenFormatsToE164()
    {
        var result = PhoneE164.Normalize("+1 234 567 8900", "US");

        result.Should().NotBeNull();
        result.Should().MatchRegex(@"^\+1\d{10}$");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenInvalidPhoneNumber_WhenNormalize_ThenReturnsNull()
    {
        PhoneE164.Normalize("123", "US").Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmptyPhoneNumber_WhenNormalize_ThenReturnsNull()
    {
        PhoneE164.Normalize("", "US").Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenInvalidPhoneNumber_WhenNormalizeOrThrow_ThenThrowsArgumentException()
    {
        FluentActions.Invoking(() => PhoneE164.NormalizeOrThrow("invalid", "US"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*invalid or cannot be normalized*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidE164_WhenIsValid_ThenReturnsTrue()
    {
        PhoneE164.IsValid("+79161234567").Should().BeTrue();
        PhoneE164.IsValid("+40722123456").Should().BeTrue();
        PhoneE164.IsValid("+12125551234").Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNonE164Formats_WhenIsValid_ThenReturnsFalse()
    {
        PhoneE164.IsValid("79161234567").Should().BeFalse();
        PhoneE164.IsValid("89161234567").Should().BeFalse();
        PhoneE164.IsValid("+7 (912) 345-67-89").Should().BeFalse();
        PhoneE164.IsValid("+7 912 345 67 89").Should().BeFalse();
        PhoneE164.IsValid("  +79161234567").Should().BeFalse();
        PhoneE164.IsValid("+79161234567 ").Should().BeFalse();
        PhoneE164.IsValid("").Should().BeFalse();
        PhoneE164.IsValid(null).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidE164_WhenRequire_ThenReturnsSame()
    {
        PhoneE164.Require("+79161234567").Should().Be("+79161234567");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNationalFormat_WhenRequire_ThenThrowsArgumentException()
    {
        FluentActions.Invoking(() => PhoneE164.Require("89161234567"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*E.164*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAlreadyE164_WhenEnsure_ThenReturnsSame()
    {
        PhoneE164.Ensure("+79161234567", "RU").Should().Be("+79161234567");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNationalFormat_WhenEnsure_ThenNormalizesToE164()
    {
        var result = PhoneE164.Ensure("9161234567", "RU");

        result.Should().Be("+79161234567");
    }
}
