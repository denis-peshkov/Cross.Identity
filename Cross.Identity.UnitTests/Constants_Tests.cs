namespace Cross.Identity.UnitTests;

using Cross.Identity;

[TestFixture]
public class Constants_Tests
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
