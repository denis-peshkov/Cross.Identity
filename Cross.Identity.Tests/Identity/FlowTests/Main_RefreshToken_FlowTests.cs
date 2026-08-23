namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_RefreshToken_FlowTests : RunFlowCommandHandlerTestsBase
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
        AddRegistryStep<RefreshTokenStepFactory>();
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
    public async Task GivenValidRefreshToken_WhenRefreshTokenFlow_ThenReturnsNewTokenPairAsync()
    {
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "refresh@example.com",
            UserName = "refresh-user",
            NormalizedUserName = "refresh-user",
        });

        var oldRefreshToken = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId,
            familyId,
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = oldRefreshToken },
            Flow,
            FlowOperationEnum.RefreshToken,
            CancellationToken.None);

        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKeys("access_token", "refresh_token", "token_type", "expires_in", "user_id");
        payload["access_token"].Should().NotBeNull();
        payload["refresh_token"].Should().NotBeNull().And.NotBe(oldRefreshToken);
        payload["token_type"].Should().Be("Bearer");
        payload["user_id"].Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidRefreshToken_WhenRefreshTokenFlow_ThenThrowsNotAuthorizedExceptionAsync()
    {
        var invalidToken = new string('x', 32);

        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = invalidToken },
            Flow,
            FlowOperationEnum.RefreshToken,
            CancellationToken.None);

        await act.Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired refresh token*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenTooShortRefreshToken_WhenRefreshTokenFlow_ThenThrowsValidationExceptionAsync()
    {
        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = "short" },
            Flow,
            FlowOperationEnum.RefreshToken,
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenReusedRefreshTokenAfterRotation_WhenRefreshTokenFlow_ThenRevokesFamilyAndThrowsConflictAsync()
    {
        // Attacker rotated first (R1 → R2); victim reuses R1 → REPLAY_DETECTED kills R2.
        var userAccountId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "replay@example.com",
            UserName = "replay-user",
            NormalizedUserName = "replay-user",
        });

        var r1 = await _jwtTokenService.GenerateRefreshTokenAsync(
            userAccountId,
            familyId,
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) }, ClientContext.Empty, CancellationToken.None);

        var first = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = r1 },
            Flow,
            FlowOperationEnum.RefreshToken,
            CancellationToken.None);

        var payload = first.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var r2 = payload["refresh_token"]!.ToString()!;

        var act = () => _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["RefreshToken"] = r1 },
            Flow,
            FlowOperationEnum.RefreshToken,
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already been used*");

        (await _jwtTokenService.ValidateRefreshTokenAsync(r2, CancellationToken.None)).Should().BeFalse();

        var familyTokens = await Context.RefreshTokens.Where(x => x.FamilyId == familyId).ToListAsync();
        familyTokens.Should().OnlyContain(t => t.RevokedAt != null);
        Context.Audits.Should().Contain(a =>
            a.RevokedReason == RefreshTokenRevokedReason.REPLAY_DETECTED
            && familyTokens.Any(t => t.Id.ToString() == a.EntityId));
    }
}
