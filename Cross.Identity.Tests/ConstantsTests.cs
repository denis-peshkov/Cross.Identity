namespace Cross.Identity.Tests;

[TestFixture]
public class ConstantsTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenClaimConstants_WhenUsername_ThenReturnsExpected()
    {
        ClaimConstants.Username.Should().Be("username");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenClaimConstants_WhenPermission_ThenReturnsExpected()
    {
        ClaimConstants.Permission.Should().Be("permission");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenClaimConstants_WhenSecurityStamp_ThenReturnsExpected()
    {
        ClaimConstants.SecurityStamp.Should().Be("security_stamp");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenIdentityConstants_WhenRequestPatchBody_ThenReturnsExpected()
    {
        IdentityConstants.RequestPatchBody.Should().Be("RequestPatchBody");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenIdentityConstants_WhenTokenNames_ThenReturnsExpected()
    {
        IdentityConstants.AccessToken.Should().Be("access_token");
        IdentityConstants.RefreshToken.Should().Be("refresh_token");
        IdentityConstants.IdToken.Should().Be("id_token");
        IdentityConstants.UserAccountId.Should().Be("user_account_id");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenExternalLoginCompletion_WhenCreated_ThenExposesValues()
    {
        var userAccountId = Guid.NewGuid();
        var completion = new ExternalLoginCompletion(userAccountId, true);

        completion.UserAccountId.Should().Be(userAccountId);
        completion.IsLinking.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenExactlyOneRequiredRule_WhenCreated_ThenStoresFields()
    {
        var rule = new ExactlyOneRequiredRule(new[] { "email", "phone" }, "pick one");

        rule.Fields.Should().Equal("email", "phone");
        rule.Message.Should().Be("pick one");
    }
}
