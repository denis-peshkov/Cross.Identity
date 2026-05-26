namespace Cross.Identity.Tests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ConstantsTests
{
    [Test]
    public void ClaimConstants_Username_ShouldReturnExpected()
    {
        ClaimConstants.Username.Should().Be("username");
    }

    [Test]
    public void ClaimConstants_Permission_ShouldReturnExpected()
    {
        ClaimConstants.Permission.Should().Be("permission");
    }

    [Test]
    public void IdentityConstants_RequestPatchBody_ShouldReturnExpected()
    {
        IdentityConstants.RequestPatchBody.Should().Be("RequestPatchBody");
    }
}
