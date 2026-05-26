namespace Cross.Identity.Tests.Services;

[Category(TestCategory.UNIT)]
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
    public void NormalizePhone_ShouldExtractDigits()
    {
        // Act
        var result = _normalizer.NormalizePhone("+1 (234) 567-8900");

        // Assert
        result.Should().Be("+12345678900");
    }

    [Test]
    public void NormalizePhone_ShouldPreservePlus()
    {
        // Act
        var result = _normalizer.NormalizePhone("+1234567890");

        // Assert
        result.Should().StartWith("+");
    }

    [Test]
    public void NormalizePhone_ShouldNotAddPlusIfMissing()
    {
        // Act
        var result = _normalizer.NormalizePhone("1234567890");

        // Assert
        result.Should().Be("1234567890");
        result.Should().NotStartWith("+");
    }

    [Test]
    public void NormalizePhone_ShouldTrimWhitespace()
    {
        // Act
        var result = _normalizer.NormalizePhone("  +1 234 567 8900  ");

        // Assert
        result.Should().Be("+12345678900");
    }

    [Test]
    public void NormalizePhone_ShouldThrowWhenTooShort()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizePhone("123456"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid phone length*");
    }

    [Test]
    public void NormalizePhone_ShouldThrowWhenTooLong()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizePhone("+1234567890123456789"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*Invalid phone length*");
    }

    [Test]
    public void NormalizeToE164_ShouldFormatValidNumber()
    {
        // Act
        var result = _normalizer.NormalizeToE164("+1 234 567 8900", "US");

        // Assert
        result.Should().NotBeNull();
        result.Should().MatchRegex(@"^\+1\d{10}$");
    }

    [Test]
    public void NormalizeToE164_ShouldReturnNullForInvalidNumber()
    {
        // Act
        var result = _normalizer.NormalizeToE164("123", "US");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void NormalizeToE164_ShouldReturnNullForEmptyString()
    {
        // Act
        var result = _normalizer.NormalizeToE164("", "US");

        // Assert
        result.Should().BeNull();
    }

    [Test]
    public void NormalizeToE164OrThrow_ShouldThrowForInvalidNumber()
    {
        // Act & Assert
        FluentActions.Invoking(() => _normalizer.NormalizeToE164OrThrow("invalid", "US"))
            .Should()
            .Throw<ArgumentException>()
            .WithMessage("*invalid or cannot be normalized*");
    }
}
