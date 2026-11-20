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
    [TestCase("second@example.com", "s***@e***.com")]
    [TestCase("a@b.co", "*@b***.co")]
    [TestCase("not-an-email", "*****")]
    public void GivenEmail_WhenMaskEmailForLog_ThenMasksLocalAndDomain(string input, string expected)
    {
        input.MaskEmailForLog().Should().Be(expected);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("+79161234567", "+791****67")]
    [TestCase("1234", "****")]
    public void GivenPhone_WhenMaskPhoneForLog_ThenMasksMiddle(string input, string expected)
    {
        input.MaskPhoneForLog().Should().Be(expected);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    [TestCase("Email", "second@example.com", "s***@e***.com")]
    [TestCase("PhoneNumber", "+79161234567", "+791****67")]
    [TestCase("Phone", "+79161234567", "+791****67")]
    [TestCase("UserName", "alice", "alice")]
    public void GivenField_WhenMaskIdentityForLog_ThenMasksEmailOrPhone(
        string field,
        string value,
        string expected)
    {
        StringExtensions.MaskIdentityForLog(field, value).Should().Be(expected);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenChannel_WhenMaskAddressForLog_ThenMasksByChannel()
    {
        ChannelEnum.Email.MaskAddressForLog("second@example.com").Should().Be("s***@e***.com");
        ChannelEnum.Sms.MaskAddressForLog("+79161234567").Should().Be("+791****67");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenSpacedInput_WhenToPascalCaseWithoutSpaces_ThenCapitalizesAfterSpaces()
    {
        "hello world".ToPascalCaseWithoutSpaces().Should().Be("HelloWorld");
    }
}
