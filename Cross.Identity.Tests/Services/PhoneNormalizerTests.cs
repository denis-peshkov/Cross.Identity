namespace Cross.Identity.Tests.Services;

[TestFixture]
public class PhoneNormalizerTests
{
    private PhoneNormalizer _normalizer = null!;

    [SetUp]
    public void SetUp()
    {
        _normalizer = new PhoneNormalizer();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenFormattedPhoneNumber_WhenNormalizePhone_ThenExtractsDigits()
    {
        // Act
        var result = _normalizer.NormalizePhone("+1 (234) 567-8900");

        // Assert
        result.Should().Be("+12345678900");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenPhoneWithPlus_WhenNormalizePhone_ThenPreservesPlus()
    {
        // Act
        var result = _normalizer.NormalizePhone("+1234567890");

        // Assert
        result.Should().StartWith("+");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenPhoneWithoutPlus_WhenNormalizePhone_ThenDoesNotAddPlus()
    {
        // Act
        var result = _normalizer.NormalizePhone("1234567890");

        // Assert
        result.Should().Be("1234567890");
        result.Should().NotStartWith("+");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenPhoneWithWhitespace_WhenNormalizePhone_ThenTrimsWhitespace()
    {
        // Act
        var result = _normalizer.NormalizePhone("  +1 234 567 8900  ");

        // Assert
        result.Should().Be("+12345678900");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenTooShortPhone_WhenNormalizePhone_ThenThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizePhone("123456"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid phone length*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenTooLongPhone_WhenNormalizePhone_ThenThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizePhone("+1234567890123456789"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid phone length*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenValidPhoneNumber_WhenNormalizeToE164_ThenFormatsToE164()
    {
        // Act
        var result = _normalizer.NormalizeToE164("+1 234 567 8900", "US");

        // Assert
        result.Should().NotBeNull();
        result.Should().MatchRegex(@"^\+1\d{10}$");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenInvalidPhoneNumber_WhenNormalizeToE164_ThenReturnsNull()
    {
        // Act
        var result = _normalizer.NormalizeToE164("123", "US");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmptyPhoneNumber_WhenNormalizeToE164_ThenReturnsNull()
    {
        // Act
        var result = _normalizer.NormalizeToE164("", "US");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenInvalidPhoneNumber_WhenNormalizeToE164OrThrow_ThenThrowsArgumentException()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizeToE164OrThrow("invalid", "US"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*invalid or cannot be normalized*");
    }
}
