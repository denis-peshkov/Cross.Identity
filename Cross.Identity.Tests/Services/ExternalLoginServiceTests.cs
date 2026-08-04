namespace Cross.Identity.Tests.Services;

[Category(TestCategory.INTEGRATION)]
[TestFixture]
public class ExternalLoginServiceTests : EFTestsBase
{
    private const string CallbackUrl = "https://app.example/callback";
    private Mock<ILogger<ExternalLoginService>> _logger = null!;
    private HttpContextAccessor _httpContextAccessor = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _logger = new Mock<ILogger<ExternalLoginService>>();
        _httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) },
            authenticationType: "Test");
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(identity);
    }

    private void ClearAuthenticatedUser()
    {
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenProviderNotSupported()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Unknown", null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not supported*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenProviderNotConfigured()
    {
        SeedProvider("Google");
        var sut = CreateService(
            new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()),
            options => options.Providers.Clear());

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*not configured*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenCallbackUrlMissing()
    {
        SeedProvider("Google");
        var sut = CreateService(
            new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()),
            options => options.CallbackUrl = string.Empty);

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*CallbackUrl*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenProviderDisabledInDatabase()
    {
        SeedProvider("Google", isEnabled: false);
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not enabled*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenProviderAlreadyLinkedToUser()
    {
        var userId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            ProviderId = providerId,
            ProviderUserId = "existing",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        });

        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));
        SetAuthenticatedUser(userId);

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, userId, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenLinkUserIdWithoutAuthentication()
    {
        SeedProvider("Google");
        ClearAuthenticatedUser();
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, Guid.NewGuid(), CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Authentication is required*");
    }

    [Test]
    public async Task InitiateAsync_ShouldThrow_WhenLinkUserIdDoesNotMatchAuthenticatedUser()
    {
        SeedProvider("Google");
        SetAuthenticatedUser(Guid.NewGuid());
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, Guid.NewGuid(), CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*does not match the authenticated user*");
    }

    [Test]
    public async Task InitiateAsync_ShouldPersistState_InDatabase()
    {
        SeedProvider("Google");
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await sut.InitiateAsync("Google", "/home", null, CancellationToken.None);

        var state = await Context.ExternalLoginStates.SingleAsync();
        state.Provider.Should().Be("Google");
        state.ReturnUrl.Should().Be("/home");
        state.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Test]
    public async Task InitiateAsync_ShouldReturnGoogleAuthorizationUrl()
    {
        SeedProvider("Google");
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        var url = await sut.InitiateAsync("Google", "/home", null, CancellationToken.None);

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.Should().Contain("client_id=google-client");
        url.Should().Contain("redirect_uri=");
        url.Should().Contain("app.example%2fcallback");
        url.Should().Contain("response_type=code");
        url.Should().Contain("scope=openid");
        url.Should().Contain("state=");
    }

    [Test]
    public async Task InitiateAsync_ShouldAddResponseMode_ForMicrosoftProvider()
    {
        SeedProvider("Microsoft");
        var sut = CreateService(
            new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()),
            options =>
            {
                options.Providers["Microsoft"] = new ExternalLoginProviderOptions
                {
                    ClientId = "ms-client",
                    ClientSecret = "ms-secret",
                };
            });

        var url = await sut.InitiateAsync("Microsoft", null, null, CancellationToken.None);

        url.Should().Contain("response_mode=query");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenProviderReturnsError()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "state", "access_denied", "User denied", CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("User denied");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenStateMissing()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", string.Empty, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*State is required*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenStateInvalid()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "not-valid-state", null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*OAuth state is invalid*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenStateExpired()
    {
        SeedProvider("Google");
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var row = await Context.ExternalLoginStates.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();

        await FluentActions.Invoking(() => sut.CompleteAsync("code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*expired*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenLinkingWithoutAuthenticatedUser()
    {
        var payload = new ExternalLoginStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Provider = "Google",
            ReturnUrl = "/account/ExternalLogins",
        };
        var state = EncodeState(payload);
        SeedOAuthState(payload);

        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.CompleteAsync("code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Authentication is required*");
    }

    [Test]
    public async Task CompleteAsync_ShouldCreateUserAndExternalLogin_ForGoogle()
    {
        SeedProvider("Google");
        var provisioner = new Mock<IExternalLoginUserProvisioner>();
        var sut = CreateService(GoogleSuccessHandler(), provisioner: provisioner.Object);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.IsLinking.Should().BeFalse();
        completion.UserId.Should().NotBe(Guid.Empty);

        var account = await Context.UsersAccounts.SingleAsync(x => x.Id == completion.UserId);
        account.Email.Should().Be("user@example.com");
        account.UserName.Should().Be("user@example.com");

        var externalLogin = await Context.UsersExternalLogins.SingleAsync();
        externalLogin.ProviderUserId.Should().Be("google-sub-1");
        externalLogin.ProviderEmail.Should().Be("user@example.com");

        provisioner.Verify(
            p => p.ProvisionAsync(completion.UserId, It.IsAny<ExternalOAuthProfile>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task CompleteAsync_ShouldReturnExistingUser_WhenExternalLoginAlreadyExists()
    {
        var userId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "linked@example.com",
            UserName = "linked",
            NormalizedUserName = "linked",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            ProviderId = providerId,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserId.Should().Be(userId);
        (await Context.UsersAccounts.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task CompleteAsync_ShouldMatchUserByEmail_WhenExternalLoginMissing()
    {
        var userId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "existing",
            NormalizedUserName = "existing",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserId.Should().Be(userId);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task CompleteAsync_ShouldLinkProviderToExistingUser()
    {
        var userId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "link@example.com",
            UserName = "link",
            NormalizedUserName = "link",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        SetAuthenticatedUser(userId);
        var url = await sut.InitiateAsync("Google", null, userId, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.IsLinking.Should().BeTrue();
        completion.UserId.Should().Be(userId);
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenLinkUserIdDoesNotMatchAuthenticatedUser()
    {
        var ownerUserId = Guid.NewGuid();
        var attackerUserId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = ownerUserId,
            Email = "owner@example.com",
            UserName = "owner",
            NormalizedUserName = "owner",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        SetAuthenticatedUser(ownerUserId);
        var url = await sut.InitiateAsync("Google", null, ownerUserId, CancellationToken.None);
        var state = ExtractState(url);

        SetAuthenticatedUser(attackerUserId);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*does not match the authenticated user*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenLinkTargetUserMissing()
    {
        var missingUserId = Guid.NewGuid();
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());
        SetAuthenticatedUser(missingUserId);
        var url = await sut.InitiateAsync("Google", null, missingUserId, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*user account was not found*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenExternalAccountLinkedToAnotherUser()
    {
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(
            new UserAccountEntity
            {
                Id = currentUserId,
                Email = "current@example.com",
                UserName = "current",
                NormalizedUserName = "current",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = otherUserId,
                Email = "other@example.com",
                UserName = "other",
                NormalizedUserName = "other",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.Empty,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = otherUserId,
            ProviderId = providerId,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        SetAuthenticatedUser(currentUserId);
        var url = await sut.InitiateAsync("Google", null, currentUserId, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked to another user*");
    }

    [Test]
    public async Task CompleteAsync_ShouldUpdateExistingExternalLogin()
    {
        var userId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            ProviderId = providerId,
            ProviderUserId = "google-sub-1",
            DisplayName = "Old Name",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastUsedAt = DateTime.UtcNow.AddDays(-1),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        var externalLogin = await Context.UsersExternalLogins.SingleAsync();
        externalLogin.DisplayName.Should().Be("Google User");
        externalLogin.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public async Task CompleteAsync_ShouldUseMicrosoftProfileEndpoint()
    {
        SeedProvider("Microsoft");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://login.microsoftonline.com/common/oauth2/v2.0/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "ms-token" }),
            ["https://graph.microsoft.com/v1.0/me"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "ms-user-1",
                    mail = "ms@example.com",
                    displayName = "MS User",
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Microsoft"] = new ExternalLoginProviderOptions
                {
                    ClientId = "ms-client",
                    ClientSecret = "ms-secret",
                };
            });

        var url = await sut.InitiateAsync("Microsoft", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserId.Should().NotBe(Guid.Empty);
        (await Context.UsersExternalLogins.SingleAsync()).ProviderUserId.Should().Be("ms-user-1");
    }

    [Test]
    public async Task CompleteAsync_ShouldFetchGitHubPrimaryEmail_WhenUserEmailMissing()
    {
        SeedProvider("GitHub");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://github.com/login/oauth/access_token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "gh-token" }),
            ["https://api.github.com/user"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = 42,
                    login = "octo",
                    avatar_url = "https://github.com/avatar.png",
                }),
            ["https://api.github.com/user/emails"] = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {"email":"secondary@example.com","primary":false},
                      {"email":"primary@example.com","primary":true}
                    ]
                    """,
                    Encoding.UTF8,
                    "application/json"),
            },
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["GitHub"] = new ExternalLoginProviderOptions
                {
                    ClientId = "gh-client",
                    ClientSecret = "gh-secret",
                };
            });

        var url = await sut.InitiateAsync("GitHub", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        (await Context.UsersAccounts.SingleAsync()).Email.Should().Be("primary@example.com");
        completion.UserId.Should().NotBe(Guid.Empty);
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenTokenExchangeFails()
    {
        SeedProvider("Google");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.BadRequest, new { error = "invalid_grant" }),
        });

        var sut = CreateService(handler);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("bad-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*token exchange*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenAccessTokenMissingInResponse()
    {
        SeedProvider("Google");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { token_type = "bearer" }),
        });

        var sut = CreateService(handler);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access_token*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenProviderUserIdMissing()
    {
        SeedProvider("Google");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "at" }),
            ["https://openidconnect.googleapis.com/v1/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { sub = string.Empty, email = "user@example.com" }),
        });

        var sut = CreateService(handler);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*user id was not returned*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenAppleProfileNotSupported()
    {
        SeedProvider("Apple");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "apple-token" }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "apple-client",
                    ClientSecret = "apple-secret",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*Apple Sign In*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenStateIsMalformedBase64()
    {
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "!!!", null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*OAuth state is invalid*");
    }

    [Test]
    public async Task CompleteAsync_ShouldThrow_WhenProviderAlreadyLinkedToAnotherAccountOnUpsert()
    {
        var userId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.Empty,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userId,
            ProviderId = providerId,
            ProviderUserId = "other-google-id",
            CreatedAt = DateTime.UtcNow,
            LastUsedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked to the current user*");
    }

    private ExternalLoginService CreateService(
        HttpMessageHandler handler,
        Action<ExternalLoginOptions>? configure = null,
        IExternalLoginUserProvisioner? provisioner = null)
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
            _logger.Object,
            _httpContextAccessor,
            provisioner);
    }

    private void SeedOAuthState(ExternalLoginStatePayload payload, TimeSpan? lifetime = null)
    {
        var now = DateTime.UtcNow;
        AddToDb(new ExternalLoginStateEntity
        {
            Nonce = payload.Nonce,
            Provider = payload.Provider,
            ReturnUrl = payload.ReturnUrl,
            LinkUserId = payload.LinkUserId,
            CreatedAt = now,
            ExpiresAt = now.Add(lifetime ?? TimeSpan.FromMinutes(10)),
        });
    }

    private short SeedProvider(string name, bool isEnabled = true)
    {
        var provider = new ProviderEntity
        {
            Name = name,
            Scheme = name.ToLowerInvariant(),
            IsEnabled = isEnabled,
            CreatedAt = DateTime.UtcNow,
        };
        AddToDb(provider);
        return provider.Id;
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
                    email = "user@example.com",
                    name = "Google User",
                    picture = "https://example.com/avatar.png",
                }),
        });

    private static string ExtractState(string authorizationUrl)
    {
        var uri = new Uri(authorizationUrl);
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        return query["state"]!;
    }

    private static string EncodeState(ExternalLoginStatePayload payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
