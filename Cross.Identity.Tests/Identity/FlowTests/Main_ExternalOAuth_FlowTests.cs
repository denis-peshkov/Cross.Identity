namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_ExternalOAuth_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string CallbackUrl = "https://app.example/callback";

    private ExternalLoginService _externalLoginService = null!;
    private HttpContextAccessor _httpContextAccessor = null!;
    private IJwtTokenService _jwtTokenService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        SeedGoogleProvider();

        _httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };

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
        _externalLoginService = CreateExternalLoginService(GoogleSuccessHandler());

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<ExternalLoginInitiateStepFactory>();
        AddRegistryStep<ExternalLoginCompleteStepFactory>();
        AddRegistryStep<ExternalLoginUnlinkStepFactory>();
        AddRegistryStep<ExternalLoginGetAllStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IExternalLoginService, IExternalLoginService>(_externalLoginService);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService());
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(_jwtTokenService);
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
            authenticationType: "Test");
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(identity);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenGoogleProvider_WhenExternalLogin_ThenReturnsAuthorizationUrlAsync()
    {
        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Provider"] = "Google",
                ["ReturnUrl"] = "/home",
            },
            Flow,
            FlowOperationEnum.ExternalLogin,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        var url = payload["url"].Should().BeOfType<string>().Subject;
        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=google-client");
        url.Should().Contain("state=");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAuthenticatedUserWithLinkedProvider_WhenExternalLoginUnlink_ThenUnlinksProviderAsync()
    {
        var userId = Guid.NewGuid();
        var provider = await Context.Providers.SingleAsync(x => x.Name == "Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "unlink@example.com",
            UserName = "unlink",
            NormalizedUserName = "unlink",
            PasswordPhc = "$pbkdf2$has-password",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            ProviderId = provider.Id,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-unlink",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId,
                ["Provider"] = "Google",
            },
            Flow,
            FlowOperationEnum.ExternalLoginUnlink,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["unlinked"].Should().Be(true);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAuthenticatedUser_WhenExternalLoginGetAll_ThenReturnsAccountEmailAndProvidersAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new ProviderEntity
        {
            Name = "Microsoft",
            Scheme = "microsoft",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        AddToDb(new ProviderEntity
        {
            Name = "GitHub",
            Scheme = "github",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
        });
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "owner@example.com",
            UserName = "owner",
            NormalizedUserName = "owner",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        var google = await Context.Providers.SingleAsync(x => x.Name == "Google");
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            ProviderId = google.Id,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            ProviderEmail = "linked@gmail.com",
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userId);

        _externalLoginService = CreateExternalLoginService(
            GoogleSuccessHandler(),
            options =>
            {
                options.Providers["Microsoft"] = new ExternalLoginProviderOptions
                {
                    ClientId = "ms-id",
                    ClientSecret = "ms-secret",
                };
            });
        RegisterToServiceProvider<IExternalLoginService, IExternalLoginService>(_externalLoginService);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["UserId"] = userId,
            },
            Flow,
            FlowOperationEnum.ExternalLoginGetAll,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["account_email"].Should().Be("owner@example.com");
        var providers = payload["providers"].Should().BeAssignableTo<IReadOnlyList<ExternalLoginProviderItemDto>>().Subject;
        providers.Should().HaveCount(2);
        providers.Should().NotContain(x => x.Provider == "GitHub");

        var googleRow = providers.Single(x => x.Provider == "Google");
        googleRow.IsConnected.Should().BeTrue();
        googleRow.ProviderEmail.Should().Be("linked@gmail.com");
        googleRow.AvatarUrl.Should().Be("https://example.com/avatar.png");

        var microsoft = providers.Single(x => x.Provider == "Microsoft");
        microsoft.IsConnected.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMatchingUserId_WhenExternalLogin_ThenAcceptsAsync()
    {
        var linkUserId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = linkUserId,
            Email = "link@example.com",
            UserName = "link",
            NormalizedUserName = "link",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(linkUserId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Provider"] = "Google",
                ["ReturnUrl"] = "/home",
                ["UserId"] = linkUserId.ToString(),
            },
            Flow,
            FlowOperationEnum.ExternalLogin,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["url"].Should().BeOfType<string>().Which.Should().Contain("state=");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserId_WhenExternalLogin_ThenAcceptsWithoutMatchingPrincipalAsync()
    {
        var authenticatedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SetAuthenticatedUser(authenticatedUserId);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Provider"] = "Google",
                ["UserId"] = otherUserId.ToString(),
            },
            Flow,
            FlowOperationEnum.ExternalLogin,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["url"].Should().BeOfType<string>().Which.Should().Contain("state=");
        (await Context.ExternalLoginStates.SingleAsync()).UserAccountId.Should().Be(otherUserId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidUserId_WhenExternalLogin_ThenThrowsValidationExceptionAsync()
    {
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(
                    new Dictionary<string, object?>
                    {
                        ["Provider"] = "Google",
                        ["UserId"] = "not-a-guid",
                    },
                    Flow,
                    FlowOperationEnum.ExternalLogin,
                    CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSuccessfulGoogleOAuth_WhenExternalLoginCallback_ThenReturnsTokensAsync()
    {
        var authorizationUrl = await _externalLoginService.InitiateAsync("Google", "/home", null, CancellationToken.None);
        var state = ExtractState(authorizationUrl);

        var result = await _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?>
            {
                ["Code"] = "auth-code",
                ["State"] = state,
            },
            Flow,
            FlowOperationEnum.ExternalLoginCallback,
            CancellationToken.None);

        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload["access_token"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        payload["refresh_token"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        payload["token_type"].Should().Be("Bearer");
        payload["expires_in"].Should().NotBeNull();
        payload["user_id"].Should().NotBeNull();
        payload["is_linking"].Should().Be(false);

        (await Context.UsersAccounts.CountAsync()).Should().Be(1);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenOAuthError_WhenExternalLoginCallback_ThenFailsAsync()
    {
        var authorizationUrl = await _externalLoginService.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(authorizationUrl);

        await FluentActions.Invoking(() => _flowExecutor.ExecuteAsync(
                new Dictionary<string, object?>
                {
                    ["Code"] = "ignored",
                    ["State"] = state,
                    ["Error"] = "access_denied",
                    ["ErrorDescription"] = "User denied access",
                },
                Flow,
                FlowOperationEnum.ExternalLoginCallback,
                CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("User denied access");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenStateAndErrorWithoutCode_WhenExternalLoginCallback_ThenProcessesErrorAsync()
    {
        var authorizationUrl = await _externalLoginService.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(authorizationUrl);

        await FluentActions.Invoking(() => _flowExecutor.ExecuteAsync(
                new Dictionary<string, object?>
                {
                    ["State"] = state,
                    ["Error"] = "access_denied",
                    ["ErrorDescription"] = "User denied access",
                },
                Flow,
                FlowOperationEnum.ExternalLoginCallback,
                CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("User denied access");
    }

    private void SeedGoogleProvider()
    {
        AddToDb(new ProviderEntity
        {
            Name = "Google",
            Scheme = "google",
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
        });
    }

    private ExternalLoginService CreateExternalLoginService(
        HttpMessageHandler handler,
        Action<ExternalLoginOptions>? configure = null)
    {
        var options = new ExternalLoginOptions
        {
            CallbackUrl = CallbackUrl,
            StateLifetime = TimeSpan.FromMinutes(10),
            Providers =
            {
                ["Google"] = new ExternalLoginProviderOptions
                {
                    ClientId = "google-client",
                    ClientSecret = "google-secret",
                },
            },
        };
        configure?.Invoke(options);

        var optionsMock = new Mock<IOptionsSnapshot<ExternalLoginOptions>>();
        optionsMock.Setup(o => o.Value).Returns(options);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(f => f.CreateClient(nameof(ExternalLoginService)))
            .Returns(new HttpClient(handler));

        return new ExternalLoginService(
            Context,
            httpClientFactory.Object,
            optionsMock.Object,
            Mock.Of<ILogger<ExternalLoginService>>(),
            _jwtTokenService,
            new CommunicationEndpointService(Context, new AuditService(Context)));
    }

    private static OAuthTestHttpHandler GoogleSuccessHandler()
        => new(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "google-token" }),
            ["https://openidconnect.googleapis.com/v1/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "google-sub-1",
                    email = "oauth-user@example.com",
                    name = "OAuth User",
                    picture = "https://example.com/avatar.png",
                }),
        });

    private static string ExtractState(string authorizationUrl)
    {
        var uri = new Uri(authorizationUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["state"]!;
    }
}
