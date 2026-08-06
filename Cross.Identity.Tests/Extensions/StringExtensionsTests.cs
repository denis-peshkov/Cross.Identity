namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public class StringExtensionsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullInput_WhenToCamelCase_ThenReturnsEmpty()
    {
        string? input = null;
        input.ToCamelCase().Should().Be("");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("SendCodeStep", "sendCodeStep")]
    [TestCase("IPAddress", "ipAddress")]
    [TestCase("URL", "url")]
    public void GivenPascalCaseInput_WhenToCamelCase_ThenConvertsCorrectly(string input, string expected)
    {
        input.ToCamelCase().Should().Be(expected);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenAlreadyLowerInput_WhenToCamelCase_ThenReturnsSame()
    {
        "alreadyLower".ToCamelCase().Should().Be("alreadyLower");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenPascalCaseInput_WhenToCamelCase1_ThenLowersFirstLetter()
    {
        "Pascal".ToCamelCase1().Should().Be("pascal");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenLowercaseInput_WhenToPascalCase_ThenUppersFirstLetter()
    {
        "camel".ToPascalCase().Should().Be("Camel");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenNullInput_WhenMaskSSN_ThenReturnsNull()
    {
        ((string?)null).MaskSSN().Should().BeNull();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmptyInput_WhenMaskSSN_ThenReturnsEmpty()
    {
        "".MaskSSN().Should().Be("");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenShortInput_WhenMaskSSN_ThenReturnsFiveStars()
    {
        "123".MaskSSN().Should().Be("*****");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenLongEnoughInput_WhenMaskSSN_ThenMasksFirstPart()
    {
        "123456789".MaskSSN().Should().Be("*****6789");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSpacedInput_WhenToPascalCaseWithoutSpaces_ThenCapitalizesAfterSpaces()
    {
        "hello world".ToPascalCaseWithoutSpaces().Should().Be("HelloWorld");
    }
}
