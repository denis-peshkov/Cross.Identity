namespace Cross.Identity.Tests.Identity.StepTests;

[Category(TestCategory.UNIT)]
[TestFixture]
public class InitiateExternalLogin_StepTests
{
    private Mock<IExternalLoginService> _externalLoginService = null!;

    [SetUp]
    public void SetUp()
    {
        _externalLoginService = new Mock<IExternalLoginService>();
    }

    [Test]
    public async Task InitiateExternalLoginStep_ShouldSetAuthorizationUrl()
    {
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", "/home", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://accounts.google.com/o/oauth2/v2/auth?state=abc");

        var step = new InitiateExternalLoginStep
        {
            Kind = "initiateExternalLogin",
            ProviderKey = "Provider",
            ReturnUrlKey = "ReturnUrl",
            ExternalLoginService = _externalLoginService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("initiateExternalLogin.Provider", "Google");
        bag.Set("initiateExternalLogin.ReturnUrl", "/home");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("initiateExternalLogin.Url").Should().Contain("accounts.google.com");
    }

    [Test]
    public async Task InitiateExternalLoginStep_ShouldReadLinkUserId_FromGuidAndString()
    {
        var linkUserId = Guid.NewGuid();
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", null, linkUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://provider/auth");

        var step = new InitiateExternalLoginStep
        {
            Kind = "initiateExternalLogin",
            ProviderKey = "Provider",
            LinkUserIdKey = "LinkUserId",
            ExternalLoginService = _externalLoginService.Object,
        };

        var bagWithGuid = new Bag();
        bagWithGuid.Set("initiateExternalLogin.Provider", "Google");
        bagWithGuid.Set("initiateExternalLogin.LinkUserId", linkUserId);
        await step.ExecuteAsync(bagWithGuid, CancellationToken.None);

        var bagWithString = new Bag();
        bagWithString.Set("initiateExternalLogin.Provider", "Google");
        bagWithString.Set("LinkUserId", linkUserId.ToString());
        await step.ExecuteAsync(bagWithString, CancellationToken.None);

        _externalLoginService.Verify(
            s => s.InitiateAsync("Google", null, linkUserId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    public async Task InitiateExternalLoginStep_ShouldIgnoreInvalidLinkUserId()
    {
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://provider/auth");

        var step = new InitiateExternalLoginStep
        {
            Kind = "initiateExternalLogin",
            ProviderKey = "Provider",
            LinkUserIdKey = "LinkUserId",
            ExternalLoginService = _externalLoginService.Object,
        };

        var bag = new Bag();
        bag.Set("initiateExternalLogin.Provider", "Google");
        bag.Set("initiateExternalLogin.LinkUserId", "not-a-guid");

        await step.ExecuteAsync(bag, CancellationToken.None);

        _externalLoginService.Verify(
            s => s.InitiateAsync("Google", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
