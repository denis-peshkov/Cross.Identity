namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ExternalLoginInitiate_StepTests
{
    private Mock<IExternalLoginService> _externalLoginService = null!;

    [SetUp]
    public void SetUp()
    {
        _externalLoginService = new Mock<IExternalLoginService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenProviderAndReturnUrl_WhenExecuteAsync_ThenSetsAuthorizationUrlAsync()
    {
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", "/home", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://accounts.google.com/o/oauth2/v2/auth?state=abc");

        var step = new ExternalLoginInitiateStep
        {
            Kind = "externalLoginInitiate",
            ProviderKey = "Provider",
            ReturnUrlKey = "ReturnUrl",
            ExternalLoginService = _externalLoginService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("externalLoginInitiate.Provider", "Google");
        bag.Set("externalLoginInitiate.ReturnUrl", "/home");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<string>("externalLoginInitiate.Url").Should().Contain("accounts.google.com");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenGuidOrStringLinkUserId_WhenExecuteAsync_ThenForwardsLinkUserIdAsync()
    {
        var linkUserId = Guid.NewGuid();
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", null, linkUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://provider/auth");

        var step = new ExternalLoginInitiateStep
        {
            Kind = "externalLoginInitiate",
            ProviderKey = "Provider",
            LinkUserIdKey = "LinkUserId",
            ExternalLoginService = _externalLoginService.Object,
        };

        var bagWithGuid = new Bag();
        bagWithGuid.Set("externalLoginInitiate.Provider", "Google");
        bagWithGuid.Set("externalLoginInitiate.LinkUserId", linkUserId);
        await step.ExecuteAsync(bagWithGuid, CancellationToken.None);

        var bagWithString = new Bag();
        bagWithString.Set("externalLoginInitiate.Provider", "Google");
        bagWithString.Set("LinkUserId", linkUserId.ToString());
        await step.ExecuteAsync(bagWithString, CancellationToken.None);

        _externalLoginService.Verify(
            s => s.InitiateAsync("Google", null, linkUserId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenInvalidLinkUserId_WhenExecuteAsync_ThenForwardsNullAsync()
    {
        _externalLoginService
            .Setup(s => s.InitiateAsync("Google", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://provider/auth");

        var step = new ExternalLoginInitiateStep
        {
            Kind = "externalLoginInitiate",
            ProviderKey = "Provider",
            LinkUserIdKey = "LinkUserId",
            ExternalLoginService = _externalLoginService.Object,
        };

        var bag = new Bag();
        bag.Set("externalLoginInitiate.Provider", "Google");
        bag.Set("externalLoginInitiate.LinkUserId", "not-a-guid");

        await step.ExecuteAsync(bag, CancellationToken.None);

        _externalLoginService.Verify(
            s => s.InitiateAsync("Google", null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
