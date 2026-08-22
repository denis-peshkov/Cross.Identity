namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class Main_CommunicationEndpoints_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";

    private CommunicationEndpointService _endpoints = null!;
    private IJwtTokenService _jwtTokenService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        Initialize();

        var optionsSnapshot = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Issuer = "http://localhost:5000",
                Audience = "http://localhost:5000",
                Key = "tTPm5yP2Q+1m7UQlM3N2AVnleqk7D4HhR0YzF9o5+Xw=",
                EncryptionKey = "r9lZJcR8CdpqgGgxP1VbUk2OQhlnwFJSwVOrMDyk4Lc=",
                UseEncryption = false,
                AccessTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(30),
            },
        });

        _jwtTokenService = new JwtTokenService(Context, new AuditService(Context), optionsSnapshot.Object);
        _endpoints = new CommunicationEndpointService(Context, new AuditService(Context), _jwtTokenService);

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<CommunicationEndpointsGetAllStepFactory>();
        AddRegistryStep<CommunicationEndpointSetPreferredStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<ICommunicationEndpointService, ICommunicationEndpointService>(_endpoints);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(_jwtTokenService);
    }

    private async Task<string> IssueRefreshTokenAsync(Guid userId)
    {
        return await _jwtTokenService.GenerateRefreshTokenAsync(
            userId,
            Guid.NewGuid(),
            new List<Claim>(),
            ClientContext.Empty,
            CancellationToken.None);
    }

    [Test]
    public async Task CommunicationEndpointsGetAll_WhenUserIdProvided_ShouldReturnEndpoints()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "c@example.com", EmailConfirmed = true });
        await _endpoints.SyncAccountContactsAsync(userId);
        var refresh = await IssueRefreshTokenAsync(userId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId.ToString(),
                ["RefreshToken"] = refresh,
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
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "a@example.com",
            PhoneNumber = "+79161234567",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
        });
        await _endpoints.SyncAccountContactsAsync(userId);
        var refresh = await IssueRefreshTokenAsync(userId);
        var all = await _endpoints.GetAllAsync(userId, refresh);
        var sms = all.Single(x => x.Channel == ChannelEnum.Sms);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId.ToString(),
                ["RefreshToken"] = refresh,
                ["EndpointId"] = sms.Id.ToString(),
                ["IpAddress"] = "10.0.0.42",
                ["UserAgent"] = "tests",
            },
            Flow,
            FlowOperationEnum.CommunicationEndpointSetPreferred,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["preferred"].Should().Be(true);
        (await _endpoints.GetPreferredAsync(userId))!.Id.Should().Be(sms.Id);
        Context.Audits.Should().Contain(a =>
            a.Operation == AuditOperation.CommunicationEndpointChanged
            && a.IpAddress == "10.0.0.42"
            && a.UserAgent == "tests");
    }

    [Test]
    public async Task CommunicationEndpointsGetAll_WhenRefreshTokenDoesNotMatchUserId_ShouldThrowNotAuthorizedAsync()
    {
        var ownerUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = ownerUserId, Email = "owner@example.com", EmailConfirmed = true });
        AddToDb(new UserAccountEntity { Id = otherUserId, Email = "other@example.com", EmailConfirmed = true });
        var ownerRefresh = await IssueRefreshTokenAsync(ownerUserId);

        await FluentActions.Invoking(() => _flowExecutor.ExecuteAsync(
                new Dictionary<string, object?>
                {
                    ["UserId"] = otherUserId.ToString(),
                    ["RefreshToken"] = ownerRefresh,
                },
                Flow,
                FlowOperationEnum.CommunicationEndpointsGetAll,
                CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("*does not match*");
    }
}
