namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class Main_VerifyToken_FlowTests : RunFlowCommandHandlerTestsBase
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

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<VerifyTokenStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(headersContextAccessor);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService(headersContextAccessor));
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

        _jwtTokenService = new JwtTokenService(Context, optionsSnapshot.Object, httpContextAccessor.Object);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(_jwtTokenService);
    }

    [Test]
    public async Task GivenValidAccessToken_WhenVerifyTokenFlow_ThenReturnsValidWithClaimsAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
            userId,
            familyId,
            new List<string>(),
            new List<Claim> { new(JwtRegisteredClaimNames.Sub, userId.ToString()) });

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["AccessToken"] = accessToken },
            Flow,
            FlowOperationEnum.VerifyToken,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["valid"].Should().Be(true);
        payload["user_id"].Should().Be(userId);
        payload.Should().ContainKey("jti");
        payload["jti"].Should().NotBeNull();
    }

    [Test]
    public async Task GivenRevokedAccessToken_WhenVerifyTokenFlow_ThenReturnsValidFalseAsync()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
            userId, familyId, new List<string>(), new List<Claim>());

        var jti = _jwtTokenService.GetClaimValue(accessToken, JwtRegisteredClaimNames.Jti);
        Guid.TryParse(jti, out var jtiGuid).Should().BeTrue();
        await _jwtTokenService.RevokeAccessTokenAsync(jtiGuid);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["AccessToken"] = accessToken },
            Flow,
            FlowOperationEnum.VerifyToken,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["valid"].Should().Be(false);
        payload.Should().NotContainKey("user_id");
    }

    [Test]
    public async Task GivenShortNonEmptyAccessToken_WhenVerifyTokenFlow_ThenReturnsValidFalseAsync()
    {
        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["AccessToken"] = "x" },
            Flow,
            FlowOperationEnum.VerifyToken,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["valid"].Should().Be(false);
        payload.Should().NotContainKey("user_id");
    }

    [Test]
    public async Task GivenMalformedAccessToken_WhenVerifyTokenFlow_ThenReturnsValidFalseAsync()
    {
        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["AccessToken"] = new string('x', 32) },
            Flow,
            FlowOperationEnum.VerifyToken,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["valid"].Should().Be(false);
    }

    [Test]
    public void GivenEmbeddedVerifyTokenDefinition_WhenParsed_ThenRequiresAccessToken()
    {
        var json = _processDefinitionProvider.GetJson(Flow, FlowOperationEnum.VerifyToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("start").GetString().Should().Be("collectForm");
        json.Should().Contain("verifyToken");
        json.Should().Contain("AccessToken");
    }
}
