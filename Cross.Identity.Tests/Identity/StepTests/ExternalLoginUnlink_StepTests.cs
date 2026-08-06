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
    public async Task GivenProvider_WhenExecuteAsync_ThenUnlinksAndSetsFlagAsync()
    {
        _externalLoginService
            .Setup(s => s.UnlinkAsync("Google", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new ExternalLoginUnlinkStep
        {
            Kind = "externalLoginUnlink",
            ProviderKey = "Provider",
            ExternalLoginService = _externalLoginService.Object,
            Next = "done",
        };

        var bag = new Bag();
        bag.Set("externalLoginUnlink.Provider", "Google");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("done");
        bag.Get<bool>("externalLoginUnlink.Unlinked").Should().BeTrue();
        _externalLoginService.Verify(s => s.UnlinkAsync("Google", It.IsAny<CancellationToken>()), Times.Once);
    }
}
