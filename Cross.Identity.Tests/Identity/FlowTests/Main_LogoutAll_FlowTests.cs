namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_LogoutAll_FlowTests : RunFlowCommandHandlerTestsBase
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
        AddRegistryStep<LogoutAllStepFactory>();
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
    public async Task GivenValidRefreshToken_WhenLogoutAllFlow_ThenRevokesAllUserTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var otherUserAccountId = Guid.NewGuid();
        var familyA = Guid.NewGuid();
        var familyB = Guid.NewGuid();

        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "logout-all@example.com",
            UserName = "logout-all",
            NormalizedUserName = "logout-all",
            SecurityStamp = Guid.NewGuid(),
        });
        AddToDb(new UserAccountEntity
        {
            Id = otherUserAccountId,
            Email = "logout-all-other@example.com",
            UserName = "logout-all-other",
            NormalizedUserName = "logout-all-other",
            SecurityStamp = Guid.NewGuid(),
        });

        var refreshA = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyA, new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);
        var refreshB = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId, familyB, new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, HostSuppliedClientContext.Empty, CancellationToken.None);
        var accessA = await _jwtTokenService.GenerateAccessTokenAsync(
            userAccountId, familyA, new List<string>(), new List<Claim>(), HostSuppliedClientContext.Empty, CancellationToken.None);
        var otherRefresh = await _jwtTokenService.GenerateRefreshTokenAsync(
            otherUserAccountId, Guid.NewGuid(), new List<Claim>(), HostSuppliedClientContext.Empty, CancellationToken.None);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = refreshA },
            Flow,
            FlowOperationEnum.LogoutAll,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["revoked"].Should().Be(true);

        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(refreshB, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateAccessTokenAsync(accessA, CancellationToken.None)).Should().BeFalse();
        (await _jwtTokenService.ValidateRefreshTokenAsync(otherRefresh, CancellationToken.None)).Should().BeTrue();

        var userRefresh = await Context.RefreshTokens.Where(x => x.UserAccountId == userAccountId).ToListAsync();
        userRefresh.Should().OnlyContain(t => t.RevokedAt != null);
        Context.Audits.Should().Contain(a =>
            a.UserAccountId == userAccountId && a.RevokedReason == RefreshTokenRevokedReason.USER_LOGOUT_ALL);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidRefreshToken_WhenLogoutAllFlow_ThenThrowsNotAuthorizedAsync()
    {
        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = new string('x', 32) },
            Flow,
            FlowOperationEnum.LogoutAll,
            CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }
}
