namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class CommunicationEndpoints_StepTests
{
    private Mock<ICommunicationEndpointService> _endpoints = null!;

    [SetUp]
    public void SetUp()
    {
        _endpoints = new Mock<ICommunicationEndpointService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdAndRefreshToken_WhenGetAll_ThenWritesEndpointsAsync()
    {
        var userId = Guid.NewGuid();
        const string refreshToken = "refresh-token-value";
        var list = new List<CommunicationEndpointDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Channel = ChannelEnum.Email,
                Address = "a@b.co",
                IsVerified = true,
                IsPreferred = true,
                Source = CommunicationEndpointSource.Account,
            },
        };
        _endpoints.Setup(s => s.GetAllAsync(userId, refreshToken, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var step = new CommunicationEndpointsGetAllStep
        {
            Kind = "communicationEndpointsGetAll",
            UserIdKey = "UserId",
            RefreshTokenKey = "RefreshToken",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointsGetAll.UserId", userId)
            .Set("communicationEndpointsGetAll.RefreshToken", refreshToken);
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<IReadOnlyList<CommunicationEndpointDto>>("communicationEndpointsGetAll.Endpoints").Should().BeEquivalentTo(list);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdEndpointAndRefreshToken_WhenSetPreferred_ThenPassesClientContextAsync()
    {
        var userId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        const string refreshToken = "refresh-token-value";
        _endpoints
            .Setup(s => s.SetPreferredAsync(userId, endpointId, refreshToken, new ClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new CommunicationEndpointSetPreferredStep
        {
            Kind = "communicationEndpointSetPreferred",
            UserIdKey = "UserId",
            EndpointIdKey = "EndpointId",
            RefreshTokenKey = "RefreshToken",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointSetPreferred.UserId", userId)
            .Set("communicationEndpointSetPreferred.EndpointId", endpointId.ToString())
            .Set("communicationEndpointSetPreferred.RefreshToken", refreshToken)
            .Set("collectForm.IpAddress", "1.2.3.4")
            .Set("collectForm.UserAgent", "ua")
            .Set("collectForm.DeviceFingerprint", "fp");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("communicationEndpointSetPreferred.Preferred").Should().BeTrue();
        _endpoints.Verify(
            s => s.SetPreferredAsync(userId, endpointId, refreshToken, new ClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
