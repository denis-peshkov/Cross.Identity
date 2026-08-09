namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class Main_CommunicationEndpoints_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private HttpContextAccessor _httpContextAccessor = null!;
    private CommunicationEndpointService _endpoints = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        Initialize();

        _httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        _endpoints = new CommunicationEndpointService(Context, _httpContextAccessor);

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<CommunicationEndpointsGetAllStepFactory>();
        AddRegistryStep<CommunicationEndpointSetPreferredStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<ICommunicationEndpointService, ICommunicationEndpointService>(_endpoints);
        RegisterToServiceProvider<IHttpContextAccessor, IHttpContextAccessor>(_httpContextAccessor);
    }

    private void SetUser(Guid userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
            authenticationType: "Test");
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task CommunicationEndpointsGetAll_WhenAuthenticated_ShouldReturnEndpoints()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        AddToDb(new UserAccountEntity { Id = userId, Email = "c@example.com", EmailConfirmed = true });
        await _endpoints.SyncAccountContactsAsync(userId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>(),
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
        var userId = Guid.NewGuid();
        SetUser(userId);
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "a@example.com",
            PhoneNumber = "+79161234567",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
        });
        await _endpoints.SyncAccountContactsAsync(userId);
        var all = await _endpoints.GetAllAsync(userId);
        var sms = all.Single(x => x.Channel == ChannelEnum.Sms);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["EndpointId"] = sms.Id.ToString() },
            Flow,
            FlowOperationEnum.CommunicationEndpointSetPreferred,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["preferred"].Should().Be(true);
        (await _endpoints.GetPreferredAsync(userId))!.Id.Should().Be(sms.Id);
    }
}
