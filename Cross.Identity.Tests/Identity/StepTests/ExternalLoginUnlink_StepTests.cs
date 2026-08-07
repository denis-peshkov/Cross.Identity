namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ExternalLoginUnlink_StepTests
{
    private Mock<IExternalLoginService> _externalLoginService = null!;

    [SetUp]
    public void SetUp()
    {
        _externalLoginService = new Mock<IExternalLoginService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenProviderAndUserId_WhenExecuteAsync_ThenUnlinksAndSetsFlagAsync()
    {
        var userId = Guid.NewGuid();
        _externalLoginService
            .Setup(s => s.UnlinkAsync("Google", userId, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new ExternalLoginUnlinkStep
        {
            Kind = "externalLoginUnlink",
            ProviderKey = "Provider",
            UserIdKey = "UserId",
            IpAddressKey = "IpAddress",
            UserAgentKey = "UserAgent",
            ExternalLoginService = _externalLoginService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("externalLoginUnlink.Provider", "Google");
        bag.Set("externalLoginUnlink.UserId", userId);

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("externalLoginUnlink.Unlinked").Should().BeTrue();
        _externalLoginService.Verify(
            s => s.UnlinkAsync("Google", userId, It.IsAny<string?>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
