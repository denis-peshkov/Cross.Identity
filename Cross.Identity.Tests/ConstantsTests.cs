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

    [Test]
    public void IdentityConstants_TokenNames_ShouldReturnExpected()
    {
        IdentityConstants.AccessToken.Should().Be("access_token");
        IdentityConstants.RefreshToken.Should().Be("refresh_token");
        IdentityConstants.IdToken.Should().Be("id_token");
        IdentityConstants.UserId.Should().Be("user_id");
        IdentityConstants.IsInvalidCode.Should().Be("is_invalid_code");
    }

    [Test]
    public void ExternalLoginCompletion_ShouldExposeValues()
    {
        var userId = Guid.NewGuid();
        var completion = new ExternalLoginCompletion(userId, true);

        completion.UserId.Should().Be(userId);
        completion.IsLinking.Should().BeTrue();
    }

    [Test]
    public void ExactlyOneRequiredRule_ShouldStoreFields()
    {
        var rule = new ExactlyOneRequiredRule(new[] { "email", "phone" }, "pick one");

        rule.Fields.Should().Equal("email", "phone");
        rule.Message.Should().Be("pick one");
    }
}
