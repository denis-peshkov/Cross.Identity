namespace Cross.Identity.Tests.Helpers;

[Category(TestCategory.UNIT)]
[TestFixture]
public class CodeGeneratorHelperTests
{
    [Test]
    public void GenerateCode_ShouldReturnRequestedLength()
    {
        var code = CodeGeneratorHelper.GenerateCode(10);

        code.Should().HaveLength(10);
        code.Should().MatchRegex("^[A-Z1-9]+$");
    }

    [Test]
    public void GenerateLetterCode_ShouldReturnUppercaseLettersOnly()
    {
        var code = CodeGeneratorHelper.GenerateLetterCode(8);

        code.Should().HaveLength(8);
        code.Should().MatchRegex("^[A-Z]+$");
    }

    [Test]
    public void GenerateNumericCode_ShouldReturnDigitsOnly()
    {
        var code = CodeGeneratorHelper.GenerateNumericCode(6);

        code.Should().HaveLength(6);
        code.Should().MatchRegex("^[1-9]+$");
    }

    [Test]
    public void GenerateHash_ShouldReturnSha256Bytes()
    {
        var hash = CodeGeneratorHelper.GenerateHash("123456");

        hash.Should().HaveCount(32);
        hash.Should().BeEquivalentTo(SHA256.HashData(Encoding.UTF8.GetBytes("123456")));
    }
}
