namespace Cross.Identity.UnitTests.Extensions;

[TestFixture]
public class StringExtensionsTests
{
    [Test]
    public void ToCamelCase_WhenNull_ReturnsEmpty()
    {
        string? input = null;
        input.ToCamelCase().Should().Be("");
    }

    [Test]
    [TestCase("SendCodeStep", "sendCodeStep")]
    [TestCase("IPAddress", "ipAddress")]
    [TestCase("URL", "url")]
    public void ToCamelCase_ShouldConvertCorrectly(string input, string expected)
    {
        input.ToCamelCase().Should().Be(expected);
    }

    [Test]
    public void ToCamelCase_WhenAlreadyLower_ReturnsSame()
    {
        "alreadyLower".ToCamelCase().Should().Be("alreadyLower");
    }

    [Test]
    public void ToCamelCase1_ShouldLowerFirstLetter()
    {
        "Pascal".ToCamelCase1().Should().Be("pascal");
    }

    [Test]
    public void ToPascalCase_ShouldUpperFirstLetter()
    {
        "camel".ToPascalCase().Should().Be("Camel");
    }

    [Test]
    public void MaskSSN_WhenNull_ReturnsNull()
    {
        ((string?)null).MaskSSN().Should().BeNull();
    }

    [Test]
    public void MaskSSN_WhenEmpty_ReturnsEmpty()
    {
        "".MaskSSN().Should().Be("");
    }

    [Test]
    public void MaskSSN_WhenShort_ReturnsFiveStars()
    {
        "123".MaskSSN().Should().Be("*****");
    }

    [Test]
    public void MaskSSN_WhenLongEnough_MasksFirstPart()
    {
        "123456789".MaskSSN().Should().Be("*****6789");
    }

    [Test]
    public void ToPascalCaseWithoutSpaces_ShouldCapitalizeAfterSpaces()
    {
        "hello world".ToPascalCaseWithoutSpaces().Should().Be("HelloWorld");
    }
}
