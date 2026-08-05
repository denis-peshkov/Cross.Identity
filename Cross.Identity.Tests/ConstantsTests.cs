namespace Cross.Identity.Tests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class ConstantsTests
{
    [Test]
    public void GivenClaimConstants_WhenUsername_ThenReturnsExpected()
    {
        ClaimConstants.Username.Should().Be("username");
    }

    [Test]
    public void GivenClaimConstants_WhenPermission_ThenReturnsExpected()
    {
        ClaimConstants.Permission.Should().Be("permission");
    }

    [Test]
    public void GivenIdentityConstants_WhenRequestPatchBody_ThenReturnsExpected()
    {
        IdentityConstants.RequestPatchBody.Should().Be("RequestPatchBody");
    }

    [Test]
    public void GivenIdentityConstants_WhenTokenNames_ThenReturnsExpected()
    {
        IdentityConstants.AccessToken.Should().Be("access_token");
        IdentityConstants.RefreshToken.Should().Be("refresh_token");
        IdentityConstants.IdToken.Should().Be("id_token");
        IdentityConstants.UserId.Should().Be("user_id");
        IdentityConstants.IsInvalidCode.Should().Be("is_invalid_code");
    }

    [Test]
    public void GivenExternalLoginCompletion_WhenCreated_ThenExposesValues()
    {
        var userId = Guid.NewGuid();
        var completion = new ExternalLoginCompletion(userId, true);

        completion.UserId.Should().Be(userId);
        completion.IsLinking.Should().BeTrue();
    }

    [Test]
    public void GivenExactlyOneRequiredRule_WhenCreated_ThenStoresFields()
    {
        var rule = new ExactlyOneRequiredRule(new[] { "email", "phone" }, "pick one");

        rule.Fields.Should().Equal("email", "phone");
        rule.Message.Should().Be("pick one");
    }
}
