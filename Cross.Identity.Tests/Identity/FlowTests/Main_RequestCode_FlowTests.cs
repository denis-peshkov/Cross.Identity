namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_RequestCode_FlowTests : RunFlowCommandHandlerTestsBase
{
    private string FLOW => "main";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        // Register step factories
        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<SendCodeStepFactory>();

        // Configure service provider to return requested services
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(
            _processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(
            CreateUserService());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true",
                ["Authentication:ClientUrl"] = "http://localhost:4200",
            })
            .Build();
        RegisterToServiceProvider<IConfiguration, IConfiguration>(configuration);
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

        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
        });
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmail_WhenExecuteRequestCodeFlow_ThenSucceedsAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Ttl"] = TimeSpan.FromMinutes(5),
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.RequestCode, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("LastCode");
        payload["LastCode"].Should().BeOfType<string>().Which.Should().HaveLength(8);
        // verify GetService<T>() calls
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Once);
        // (optional) verify total number of invocations
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(9);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSubmittedTtl_WhenExecuteRequestCodeFlow_ThenPersistsMatchingExpiresAtAsync()
    {
        var ttl = TimeSpan.FromMinutes(17);
        var before = DateTime.UtcNow;

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Email"] = "test@example.com",
                ["Ttl"] = ttl,
            },
            FLOW,
            FlowOperationEnum.RequestCode,
            CancellationToken.None);

        var after = DateTime.UtcNow;
        result.Data.Should().NotBeNull();

        var entity = await Context.EmailVerifications.SingleAsync();
        entity.ExpiresAt.Should().BeOnOrAfter(before.Add(ttl).AddSeconds(-1));
        entity.ExpiresAt.Should().BeOnOrBefore(after.Add(ttl).AddSeconds(1));
        entity.ExpiresAt.Should().BeCloseTo(entity.CreatedAt.Add(ttl), TimeSpan.FromSeconds(2));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidEmail_WhenExecuteRequestCodeFlow_ThenThrowsValidationExceptionAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.RequestCode, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // verify that an error message is present
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnknownEmail_WhenExecuteRequestCodeFlow_ThenReturnsInvalidCredentialsAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "unknown@example.com",
            ["Ttl"] = TimeSpan.FromMinutes(5),
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.RequestCode, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("Invalid credentials.");
    }
}
