namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class Main_CommunicationEndpoints_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";

    private CommunicationEndpointService _endpoints = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        Initialize();

        _endpoints = new CommunicationEndpointService(Context, new AuditService(Context), TestAuthOptions.Snapshot());

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<CommunicationEndpointsGetAllStepFactory>();
        AddRegistryStep<CommunicationEndpointSetPreferredStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<ICommunicationEndpointService, ICommunicationEndpointService>(_endpoints);
    }

    [Test]
    public async Task CommunicationEndpointsGetAll_WhenUserIdProvided_ShouldReturnEndpoints()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "c@example.com", EmailVerified = true });
        await _endpoints.SyncAccountContactsAsync(userAccountId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserAccountId"] = userAccountId.ToString(),
            },
            Flow,
            FlowOperationEnum.CommunicationEndpointsGetAll,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var list = payload["endpoints"].Should().BeAssignableTo<IReadOnlyList<CommunicationEndpointDto>>().Subject;
        list.Should().ContainSingle(x => x.Channel == ChannelEnum.Email && x.IsPreferred);
    }

    [Test]
    public async Task CommunicationEndpointSetPreferred_ShouldSwitchPreferred()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "a@example.com",
            PhoneNumber = "+79161234567",
            EmailVerified = true,
            PhoneNumberVerified = true,
        });
        await _endpoints.SyncAccountContactsAsync(userAccountId);
        var all = await _endpoints.GetAllAsync(userAccountId);
        var sms = all.Single(x => x.Channel == ChannelEnum.Sms);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserAccountId"] = userAccountId.ToString(),
                ["EndpointId"] = sms.Id.ToString(),
                ["IpAddress"] = "10.0.0.42",
                ["UserAgent"] = "tests",
            },
            Flow,
            FlowOperationEnum.CommunicationEndpointSetPreferred,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["preferred"].Should().Be(true);
        (await _endpoints.GetPreferredAsync(userAccountId))!.Id.Should().Be(sms.Id);
        Context.Audits.Should().Contain(a =>
            a.Operation == AuditOperation.CommunicationEndpointChanged
            && a.IpAddress == "10.0.0.42"
            && a.UserAgent == "tests");
    }
}
