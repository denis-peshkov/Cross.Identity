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
    public async Task GivenUserId_WhenGetAll_ThenWritesEndpointsAsync()
    {
        var userAccountId = Guid.NewGuid();
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
        _endpoints.Setup(s => s.GetAllAsync(userAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var step = new CommunicationEndpointsGetAllStep
        {
            Kind = "communicationEndpointsGetAll",
            UserAccountIdKey = "UserAccountId",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointsGetAll.UserAccountId", userAccountId);
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<IReadOnlyList<CommunicationEndpointDto>>("communicationEndpointsGetAll.Endpoints").Should().BeEquivalentTo(list);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdAndEndpoint_WhenSetPreferred_ThenPassesHostSuppliedClientContextAsync()
    {
        var userAccountId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        _endpoints
            .Setup(s => s.SetPreferredAsync(userAccountId, endpointId, new HostSuppliedClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new CommunicationEndpointSetPreferredStep
        {
            Kind = "communicationEndpointSetPreferred",
            UserAccountIdKey = "UserAccountId",
            EndpointIdKey = "EndpointId",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointSetPreferred.UserAccountId", userAccountId)
            .Set("communicationEndpointSetPreferred.EndpointId", endpointId.ToString())
            .Set("collectForm.IpAddress", "1.2.3.4")
            .Set("collectForm.UserAgent", "ua")
            .Set("collectForm.DeviceFingerprint", "fp");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("communicationEndpointSetPreferred.Preferred").Should().BeTrue();
        _endpoints.Verify(
            s => s.SetPreferredAsync(userAccountId, endpointId, new HostSuppliedClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
