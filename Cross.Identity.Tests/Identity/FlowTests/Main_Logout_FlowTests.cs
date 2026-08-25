namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_Logout_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string SignKeyBase64 = "tTPm5yP2Q+1m7UQlM3N2AVnleqk7D4HhR0YzF9o5+Xw=";
    private const string EncKeyBase64 = "r9lZJcR8CdpqgGgxP1VbUk2OQhlnwFJSwVOrMDyk4Lc=";

    private JwtTokenService _jwtTokenService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<LogoutStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService());
        RegisterToServiceProvider<IdentityContext, IdentityContext>(Context);

        var optionsSnapshot = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Issuer = "http://localhost:5000",
                Audience = "http://localhost:5000",
                Key = SignKeyBase64,
                EncryptionKey = EncKeyBase64,
                UseEncryption = false,
                AccessTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(30),
            },
        });
        RegisterToServiceProvider<IOptionsSnapshot<AuthenticationOptions>, IOptionsSnapshot<AuthenticationOptions>>(optionsSnapshot.Object);

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        _jwtTokenService = new JwtTokenService(Context, new AuditService(Context), optionsSnapshot.Object);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(_jwtTokenService);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidJti_WhenLogoutFlow_ThenRevokesOnlyThatSessionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyA = Guid.NewGuid();
        var familyB = Guid.NewGuid();

        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "logout@example.com",
            UserName = "logout-user",
            NormalizedUserName = "logout-user",
        });

        var refreshA = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyA, new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);
        var refreshB = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyB, new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);
        var accessA = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, familyA, new List<string>(), new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);
        var accessB = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, familyB, new List<string>(), new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);

        var jtiA = Guid.Parse(_jwtTokenService.GetClaimValue(accessA, JwtRegisteredClaimNames.Jti)!);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Jti"] = jtiA.ToString() },
            Flow,
            FlowOperationEnum.Logout,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["revoked"].Should().Be(true);

        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshB, CancellationToken.None)).Should().BeTrue();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessB, CancellationToken.None)).Should().BeTrue();

        var entityA = await Context.RefreshTokens.SingleAsync(x =>
            x.TokenHash == Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshA))));
        entityA.RevokedAt.Should().NotBeNull();
        Context.Audits.Should().Contain(a =>
            a.EntityId == entityA.Id.ToString()
            && a.RevokedReason == RefreshTokenRevokedReason.USER_LOGOUT);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnknownJti_WhenLogoutFlow_ThenSucceedsIdempotentlyAsync()
    {
        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Jti"] = Guid.NewGuid().ToString() },
            Flow,
            FlowOperationEnum.Logout,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["revoked"].Should().Be(true);
    }
}
