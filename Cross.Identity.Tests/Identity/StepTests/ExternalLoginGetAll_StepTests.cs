namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class ExternalLoginGetAll_StepTests
{
    private Mock<IExternalLoginService> _externalLoginService = null!;

    [SetUp]
    public void SetUp()
    {
        _externalLoginService = new Mock<IExternalLoginService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenOverview_WhenExecuteAsync_ThenWritesAccountEmailAndProvidersAsync()
    {
        var userId = Guid.NewGuid();
        var overview = new ExternalLoginOverviewDto
        {
            AccountEmail = "owner@example.com",
            Providers = new List<ExternalLoginProviderItemDto>
            {
                new()
                {
                    Provider = "Google",
                    DisplayName = "Google",
                    IsConnected = true,
                    ProviderEmail = "g@example.com",
                    AvatarUrl = "https://example.com/a.png",
                },
            },
        };
        _externalLoginService
            .Setup(s => s.GetAllAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        var step = new ExternalLoginGetAllStep
        {
            Kind = "externalLoginGetAll",
            UserIdKey = "UserId",
            ExternalLoginService = _externalLoginService.Object,
            Next = "collectResult",
        };

        var bag = new Bag();
        bag.Set("externalLoginGetAll.UserId", userId);
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        result.Next.Should().Be("collectResult");
        bag.Get<string?>("externalLoginGetAll.AccountEmail").Should().Be("owner@example.com");
        bag.Get<IReadOnlyList<ExternalLoginProviderItemDto>>("externalLoginGetAll.Providers")
            .Should()
            .BeEquivalentTo(overview.Providers);
        _externalLoginService.Verify(s => s.GetAllAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
