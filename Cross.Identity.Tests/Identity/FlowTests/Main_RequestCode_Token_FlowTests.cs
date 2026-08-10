namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_RequestCode_Token_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private Guid _userId;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<SendCodeStepFactory>();
        AddRegistryStep<TokenStepFactory>();
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                configuration));

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
            }
        });
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        context.Request.Headers["User-Agent"] = "MyTestUA";
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(
            new JwtTokenService(Context, new AuditService(Context), optionsSnapshot.Object));

        _userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = _userId,
            Email = "test@example.com",
        });
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRequestCodeThenToken_WhenExecuted_ThenSucceedsAsync()
    {
        var email = "test@example.com";

        var requestCodeResult = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = email,
                ["Ttl"] = TimeSpan.FromMinutes(5),
            },
            Flow,
            FlowOperationEnum.RequestCode,
            CancellationToken.None);

        var requestPayload = requestCodeResult.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        requestPayload.Should().ContainKey("LastCode");

        var lastCode = requestPayload["LastCode"]
            .Should().BeOfType<string>().Subject;

        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = _userId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash(lastCode),
            TokenLength = (byte)lastCode.Length,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var tokenResult = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = email,
                ["Code"] = lastCode,
            },
            Flow,
            FlowOperationEnum.Token,
            CancellationToken.None);

        var tokens = tokenResult.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        tokens.Should().ContainKey("access_token");
        tokens.Should().ContainKey("refresh_token");
        tokens.Should().ContainKey("user_id");
        tokens["is_invalid_code"].Should().Be(false);
        tokens["user_id"].Should().Be(_userId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSecondRequestCode_WhenTokenFlow_ThenUsesLatestCodeAsync()
    {
        var email = "test@example.com";

        await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = email,
                ["Ttl"] = TimeSpan.FromMinutes(5),
            },
            Flow,
            FlowOperationEnum.RequestCode,
            CancellationToken.None);

        var secondRequest = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = email,
                ["Ttl"] = TimeSpan.FromMinutes(5),
            },
            Flow,
            FlowOperationEnum.RequestCode,
            CancellationToken.None);

        var secondPayload = secondRequest.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var latestCode = secondPayload["LastCode"]
            .Should().BeOfType<string>().Subject;

        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = _userId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("OLD11111"),
            TokenLength = 8,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-1)
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = _userId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash(latestCode),
            TokenLength = (byte)latestCode.Length,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var tokenResult = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = email,
                ["Code"] = latestCode,
            },
            Flow,
            FlowOperationEnum.Token,
            CancellationToken.None);

        var tokens = tokenResult.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        tokens["is_invalid_code"].Should().Be(false);
        tokens.Should().ContainKey("access_token");
    }
}
