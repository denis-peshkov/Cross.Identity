namespace Cross.Identity.Tests.Extensions;

[Category(TestCategory.UNIT)]
[TestFixture]
public class StringExtensionsTests
{
    [Test]
    public void GivenNullInput_WhenToCamelCase_ThenReturnsEmpty()
    {
        string? input = null;
        input.ToCamelCase().Should().Be("");
    }

    [Test]
    [TestCase("SendCodeStep", "sendCodeStep")]
    [TestCase("IPAddress", "ipAddress")]
    [TestCase("URL", "url")]
    public void GivenPascalCaseInput_WhenToCamelCase_ThenConvertsCorrectly(string input, string expected)
    {
        input.ToCamelCase().Should().Be(expected);
    }

    [Test]
    public void GivenAlreadyLowerInput_WhenToCamelCase_ThenReturnsSame()
    {
        "alreadyLower".ToCamelCase().Should().Be("alreadyLower");
    }

    [Test]
    public void GivenPascalCaseInput_WhenToCamelCase1_ThenLowersFirstLetter()
    {
        "Pascal".ToCamelCase1().Should().Be("pascal");
    }

    [Test]
    public void GivenLowercaseInput_WhenToPascalCase_ThenUppersFirstLetter()
    {
        "camel".ToPascalCase().Should().Be("Camel");
    }

    [Test]
    public void GivenNullInput_WhenMaskSSN_ThenReturnsNull()
    {
        ((string?)null).MaskSSN().Should().BeNull();
    }

    [Test]
    public void GivenEmptyInput_WhenMaskSSN_ThenReturnsEmpty()
    {
        "".MaskSSN().Should().Be("");
    }

    [Test]
    public void GivenShortInput_WhenMaskSSN_ThenReturnsFiveStars()
    {
        "123".MaskSSN().Should().Be("*****");
    }

    [Test]
    public void GivenLongEnoughInput_WhenMaskSSN_ThenMasksFirstPart()
    {
        "123456789".MaskSSN().Should().Be("*****6789");
    }

    [Test]
    public void GivenSpacedInput_WhenToPascalCaseWithoutSpaces_ThenCapitalizesAfterSpaces()
    {
        "hello world".ToPascalCaseWithoutSpaces().Should().Be("HelloWorld");
    }
}
