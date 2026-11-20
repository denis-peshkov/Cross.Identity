namespace Cross.Identity.Tests.Services;

[TestFixture]
public class ExternalLoginServiceTests : EFTestsBase
{
    private const string CallbackUrl = "https://app.example/auth/external-login-callback";
    private const string FormCallbackUrl = "https://api.example/api/v1/Identity/ExternalLogin/OAuthReturn";
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

    private void SetAuthenticatedUser(Guid userAccountId)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(JwtRegisteredClaimNames.Sub, userAccountId.ToString()) },
            authenticationType: "Test");
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(identity);
    }

    private void ClearAuthenticatedUser()
    {
        _httpContextAccessor.HttpContext!.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    private JwtTokenService CreateJwtTokenService()
    {
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
        return new JwtTokenService(Context, new AuditService(Context), optionsSnapshot.Object);
    }

    private static UserAccountEntity NewUser(Guid id, string email = "user@example.com")
    {
        return new UserAccountEntity
        {
            Id = id,
            Email = email,
            UserName = email,
            NormalizedUserName = email,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        };
    }


    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedProvider_WhenInitiateAsync_ThenThrowsNotFoundAsync()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Unknown", null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not supported*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnconfiguredProvider_WhenInitiateAsync_ThenThrowsValidationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingCallbackUrl_WhenInitiateAsync_ThenThrowsInvalidOperationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleFormCallbackUrlWithoutCallbackUrl_WhenInitiateAsync_ThenUsesFormCallbackUrlAsync()
    {
        SeedProvider("Apple");
        var sut = CreateService(
            new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()),
            options =>
            {
                options.CallbackUrl = string.Empty;
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);

        var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
        query["redirect_uri"].Should().Be(FormCallbackUrl);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleMissingFormCallbackUrl_WhenInitiateAsync_ThenThrowsInvalidOperationExceptionAsync()
    {
        SeedProvider("Apple");
        var sut = CreateService(
            new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()),
            options =>
            {
                // CallbackUrl alone must not satisfy Apple.
                options.FormCallbackUrl = string.Empty;
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        await FluentActions.Invoking(() => sut.InitiateAsync("Apple", null, null, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*FormCallbackUrl*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenDisabledProvider_WhenInitiateAsync_ThenThrowsNotFoundAsync()
    {
        SeedProvider("Google", isEnabled: false);
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not enabled*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptyUserAccountId_WhenInitiateAsyncForLinking_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Google");
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, Guid.Empty, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*UserAccountId is required*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAlreadyLinkedProvider_WhenInitiateAsync_ThenThrowsValidationExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "existing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        AddToDb(NewUser(userAccountId));
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, userAccountId, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserAccountId_WhenInitiateAsyncForLinking_ThenPersistsUserIdAsync()
    {
        var linkUserAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(NewUser(linkUserAccountId));
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await sut.InitiateAsync("Google", null, linkUserAccountId, CancellationToken.None);

        var state = await Context.ExternalLoginStates.SingleAsync();
        state.UserAccountId.Should().Be(linkUserAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidProvider_WhenInitiateAsync_ThenPersistsStateInDatabaseAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenGoogleProvider_WhenInitiateAsync_ThenReturnsAuthorizationUrlAsync()
    {
        SeedProvider("Google");
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        var url = await sut.InitiateAsync("Google", "/home", null, CancellationToken.None);

        url.Should().StartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
        query["client_id"].Should().Be("google-client");
        query["redirect_uri"].Should().Be(CallbackUrl);
        query["response_type"].Should().Be("code");
        query["scope"].Should().Contain("openid");
        query["state"].Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMicrosoftProvider_WhenInitiateAsync_ThenAddsResponseModeQueryAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenProviderError_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "state", "access_denied", "User denied", CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("User denied");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingState_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", string.Empty, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*State is required*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidState_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>()));

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "not-valid-state", null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*OAuth state is invalid*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredState_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnauthenticatedUser_WhenCompleteAsyncForLinking_ThenThrowsNotAuthorizedAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenGoogleSuccessFlow_WhenCompleteAsync_ThenCreatesUserAndExternalLoginAsync()
    {
        SeedProvider("Google");
        var provisioner = new Mock<IExternalLoginUserProvisioner>();
        var sut = CreateService(GoogleSuccessHandler(), provisioner: provisioner.Object);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.IsLinking.Should().BeFalse();
        completion.UserAccountId.Should().NotBe(Guid.Empty);

        var account = await Context.UsersAccounts.SingleAsync(x => x.Id == completion.UserAccountId);
        account.Email.Should().Be("user@example.com");
        account.EmailVerified.Should().BeTrue();
        account.UserName.Should().Be("user@example.com");

        var externalLogin = await Context.UsersExternalLogins.SingleAsync();
        externalLogin.ProviderUserId.Should().Be("google-sub-1");
        externalLogin.ProviderEmail.Should().Be("user@example.com");

        provisioner.Verify(
            p => p.ProvisionAsync(completion.UserAccountId, It.IsAny<ExternalOAuthProfile>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUsedState_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*expired or was already used*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingExternalLogin_WhenCompleteAsync_ThenReturnsExistingUserAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "linked@example.com",
            UserName = "linked",
            NormalizedUserName = "linked",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserAccountId.Should().Be(userAccountId);
        (await Context.UsersAccounts.CountAsync()).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveLinkedAccount_WhenOAuthSignIn_ThenThrowsNotAuthorizedAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "existing",
            NormalizedUserName = "existing",
            EmailVerified = true,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Account is disabled*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedEmailSquat_WhenOAuthWithVerifiedEmail_ThenCreatesVerifiedAccountAsync()
    {
        var squatterUserAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = squatterUserAccountId,
            Email = "user@example.com",
            UserName = "squatter",
            NormalizedUserName = "squatter",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserAccountId.Should().NotBe(squatterUserAccountId);
        (await Context.UsersAccounts.CountAsync()).Should().Be(2);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(1);
        var oauthAccount = await Context.UsersAccounts.SingleAsync(x => x.Id == completion.UserAccountId);
        oauthAccount.EmailVerified.Should().BeTrue();
        (await Context.UsersAccounts.SingleAsync(x => x.Id == squatterUserAccountId)).EmailVerified.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenVerifiedEmailAccount_WhenOAuthWithUnverifiedEmail_ThenDoesNotLinkAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "existing",
            NormalizedUserName = "existing",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "google-token" }),
            ["https://openidconnect.googleapis.com/v1/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "google-sub-1",
                    email = "user@example.com",
                    email_verified = false,
                    name = "Google User",
                }),
        });

        var sut = CreateService(handler);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*email already exists*");

        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedEmailSquat_WhenOAuthWithUnverifiedEmail_ThenDoesNotCreateSecondAccountAsync()
    {
        var squatterUserAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = squatterUserAccountId,
            Email = "user@example.com",
            UserName = "squatter",
            NormalizedUserName = "squatter",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://oauth2.googleapis.com/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "google-token" }),
            ["https://openidconnect.googleapis.com/v1/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "google-sub-squat",
                    email = "user@example.com",
                    email_verified = false,
                    name = "Google User",
                }),
        });

        var sut = CreateService(handler);
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*email already exists*");

        (await Context.UsersAccounts.CountAsync()).Should().Be(1);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedProviderInState_WhenCompleteAsync_ThenThrowsNotFoundExceptionAsync()
    {
        var payload = new ExternalLoginStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Provider = "NotARealProvider",
            ReturnUrl = null,
        };
        var state = EncodeState(payload);
        SeedOAuthState(payload);
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*not supported*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenProviderNotConfigured_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Google");
        var payload = new ExternalLoginStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Provider = "Google",
            ReturnUrl = null,
        };
        var state = EncodeState(payload);
        SeedOAuthState(payload);
        var sut = CreateService(
            GoogleSuccessHandler(),
            options => options.Providers.Clear());

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*not configured*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMatchingEmailWithoutExternalLogin_WhenCompleteAsync_ThenLinksToExistingUserAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "existing",
            NormalizedUserName = "existing",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.UserAccountId.Should().Be(userAccountId);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAuthenticatedUser_WhenCompleteAsyncForLinking_ThenLinksProviderAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "link@example.com",
            UserName = "link",
            NormalizedUserName = "link",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, userAccountId, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.IsLinking.Should().BeTrue();
        completion.UserAccountId.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUserIdInState_WhenCompleteAsyncForLinking_ThenLinksWithoutHttpContextPrincipalAsync()
    {
        var ownerUserAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = ownerUserAccountId,
            Email = "owner@example.com",
            UserName = "owner",
            NormalizedUserName = "owner",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, ownerUserAccountId, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        completion.IsLinking.Should().BeTrue();
        completion.UserAccountId.Should().Be(ownerUserAccountId);
        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == ownerUserAccountId)).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingLinkTargetUser_WhenInitiateAsyncForLinking_ThenThrowsNotFoundAsync()
    {
        var missingUserAccountId = Guid.NewGuid();
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.InitiateAsync("Google", null, missingUserAccountId, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*user account was not found*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExternalAccountLinkedToAnotherUser_WhenCompleteAsyncForLinking_ThenThrowsValidationExceptionWithMergeTokenAsync()
    {
        var currentUserAccountId = Guid.NewGuid();
        var otherUserAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(
            new UserAccountEntity
            {
                Id = currentUserAccountId,
                Email = "current@example.com",
                UserName = "current",
                NormalizedUserName = "current",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = otherUserAccountId,
                Email = "other@example.com",
                UserName = "other",
                NormalizedUserName = "other",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });
        AddToDb(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = otherUserAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, currentUserAccountId, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already linked to another user*");

        AccountMergeRequired.TryGetMergeToken(ex.Which, out var mergeToken).Should().BeTrue();
        mergeToken.Should().NotBeNullOrWhiteSpace();
        ex.Which.Data["SubCode"].Should().Be(AccountMergeRequired.ErrorCode);
        ex.Which.Data[AccountMergeRequired.AbsorbedUserAccountIdKey].Should().Be(otherUserAccountId.ToString("D"));
        ex.Which.Data[AccountMergeRequired.SurvivorUserAccountIdKey].Should().Be(currentUserAccountId.ToString("D"));
        ex.Which.Data[AccountMergeRequired.ProviderKey].Should().Be("Google");

        (await Context.AccountMergePendings.CountAsync(x => x.MergeToken == mergeToken && x.ConsumedAt == null))
            .Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMergeTokenFromLinkConflict_WhenMergeAccountsAsync_ThenMovesExternalsAndDeactivatesAbsorbedAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var googleId = SeedProvider("Google");
        var microsoftId = SeedProvider("Microsoft");

        AddToDb(
            new UserAccountEntity
            {
                Id = survivorId,
                Email = "survivor@example.com",
                UserName = "survivor",
                NormalizedUserName = "survivor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = absorbedId,
                Email = "absorbed@example.com",
                UserName = "absorbed",
                NormalizedUserName = "absorbed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });

        AddToDb(
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = survivorId,
                UserAccount = null!,
                ProviderId = googleId,
                ProviderEntity = null!,
                ProviderUserId = "google-survivor",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = absorbedId,
                UserAccount = null!,
                ProviderId = microsoftId,
                ProviderEntity = null!,
                ProviderUserId = "microsoft-absorbed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        var mergeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        AddToDb(new AccountMergePendingEntity
        {
            Id = Guid.NewGuid(),
            MergeToken = mergeToken,
            SurvivorUserAccountId = survivorId,
            AbsorbedUserAccountId = absorbedId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.RevokeAllTokensForUserAsync(
                absorbedId,
                RefreshTokenRevokedReason.ACCOUNT_MERGED,
                It.IsAny<HostSuppliedClientContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        var result = await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        result.SurvivorUserAccountId.Should().Be(survivorId);
        result.AbsorbedUserAccountId.Should().Be(absorbedId);
        result.MovedProviders.Should().BeEquivalentTo("Microsoft");

        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == survivorId)).Should().Be(2);
        (await Context.UsersExternalLogins.AnyAsync(x => x.UserAccountId == survivorId && x.ProviderId == microsoftId))
            .Should().BeTrue();
        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == absorbedId)).Should().Be(0);

        var absorbed = await Context.UsersAccounts.SingleAsync(x => x.Id == absorbedId);
        absorbed.IsActive.Should().BeFalse();

        var pending = await Context.AccountMergePendings.SingleAsync(x => x.MergeToken == mergeToken);
        pending.ConsumedAt.Should().NotBeNull();

        var audit = await Context.Audits.SingleAsync(x => x.Operation == AuditOperation.UserAccountsMerged);
        audit.UserAccountId.Should().Be(survivorId);
        audit.EntityType.Should().Be(AuditEntityType.UserAccount);
        audit.EntityId.Should().Be(absorbedId.ToString("D"));
        audit.Notes.Should().Contain("MovedProviders=Microsoft");

        jwt.Verify(x => x.RevokeAllTokensForUserAsync(
            absorbedId,
            RefreshTokenRevokedReason.ACCOUNT_MERGED,
            It.IsAny<HostSuppliedClientContext>(),
            It.IsAny<CancellationToken>()), Times.Once);

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                survivorId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid or has expired*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAbsorbedHasMultipleExternals_WhenMergeAccountsAsync_ThenMovesAllProvidersAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var googleId = SeedProvider("Google");
        var microsoftId = SeedProvider("Microsoft");
        var appleId = SeedProvider("Apple");

        AddToDb(
            new UserAccountEntity
            {
                Id = survivorId,
                Email = "survivor-multi@example.com",
                UserName = "survivor-multi",
                NormalizedUserName = "survivor-multi",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = absorbedId,
                Email = "absorbed-multi@example.com",
                UserName = "absorbed-multi",
                NormalizedUserName = "absorbed-multi",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });

        AddToDb(
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = survivorId,
                UserAccount = null!,
                ProviderId = googleId,
                ProviderEntity = null!,
                ProviderUserId = "google-survivor-multi",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = absorbedId,
                UserAccount = null!,
                ProviderId = microsoftId,
                ProviderEntity = null!,
                ProviderUserId = "microsoft-absorbed-multi",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = absorbedId,
                UserAccount = null!,
                ProviderId = appleId,
                ProviderEntity = null!,
                ProviderUserId = "apple-absorbed-multi",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });

        var mergeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        AddToDb(new AccountMergePendingEntity
        {
            Id = Guid.NewGuid(),
            MergeToken = mergeToken,
            SurvivorUserAccountId = survivorId,
            AbsorbedUserAccountId = absorbedId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.RevokeAllTokensForUserAsync(
                absorbedId,
                RefreshTokenRevokedReason.ACCOUNT_MERGED,
                It.IsAny<HostSuppliedClientContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        var result = await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        result.SurvivorUserAccountId.Should().Be(survivorId);
        result.AbsorbedUserAccountId.Should().Be(absorbedId);
        result.MovedProviders.Should().BeEquivalentTo("Microsoft", "Apple");

        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == survivorId)).Should().Be(3);
        (await Context.UsersExternalLogins.AnyAsync(x => x.UserAccountId == survivorId && x.ProviderId == microsoftId))
            .Should().BeTrue();
        (await Context.UsersExternalLogins.AnyAsync(x => x.UserAccountId == survivorId && x.ProviderId == appleId))
            .Should().BeTrue();
        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == absorbedId)).Should().Be(0);

        var absorbed = await Context.UsersAccounts.SingleAsync(x => x.Id == absorbedId);
        absorbed.IsActive.Should().BeFalse();

        var audit = await Context.Audits.SingleAsync(x => x.Operation == AuditOperation.UserAccountsMerged);
        audit.Notes.Should().Match(n =>
            n.Contains("MovedProviders=", StringComparison.Ordinal)
            && n.Contains("Microsoft", StringComparison.Ordinal)
            && n.Contains("Apple", StringComparison.Ordinal));

        jwt.Verify(x => x.RevokeAllTokensForUserAsync(
            absorbedId,
            RefreshTokenRevokedReason.ACCOUNT_MERGED,
            It.IsAny<HostSuppliedClientContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMergeTokenForOtherSurvivor_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var survivorId = Guid.NewGuid();
        var otherSurvivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();

        AddToDb(
            new UserAccountEntity
            {
                Id = survivorId,
                Email = "survivor@example.com",
                UserName = "survivor",
                NormalizedUserName = "survivor",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = otherSurvivorId,
                Email = "other@example.com",
                UserName = "other",
                NormalizedUserName = "other",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = absorbedId,
                Email = "absorbed@example.com",
                UserName = "absorbed",
                NormalizedUserName = "absorbed",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });

        var mergeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        AddToDb(new AccountMergePendingEntity
        {
            Id = Guid.NewGuid(),
            MergeToken = mergeToken,
            SurvivorUserAccountId = survivorId,
            AbsorbedUserAccountId = absorbedId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                otherSurvivorId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*does not belong to the current user*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptyMergeToken_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                "   ",
                Guid.NewGuid(),
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Merge token is required*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptySurvivorUserAccountId_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                Guid.Empty,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*UserAccountId is required*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredMergeToken_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        AddToDb(NewUser(survivorId, "survivor-expired@example.com"), NewUser(absorbedId, "absorbed-expired@example.com"));
        var mergeToken = SeedMergePending(survivorId, absorbedId, expiresAt: DateTime.UtcNow.AddMinutes(-1));
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                survivorId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*invalid or has expired*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSelfMergePending_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(NewUser(userId, "self-merge@example.com"));
        var mergeToken = SeedMergePending(userId, userId);
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                userId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Cannot merge an account into itself*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAbsorbedAccountMissing_WhenMergeAccountsAsync_ThenThrowsValidationExceptionAsync()
    {
        var survivorId = Guid.NewGuid();
        var missingAbsorbedId = Guid.NewGuid();
        AddToDb(NewUser(survivorId, "survivor-missing-b@example.com"));
        var mergeToken = SeedMergePending(survivorId, missingAbsorbedId);
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                survivorId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*Absorbed user account was not found*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSurvivorInactive_WhenMergeAccountsAsync_ThenThrowsNotAuthorizedAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var survivor = NewUser(survivorId, "survivor-inactive@example.com");
        survivor.IsActive = false;
        AddToDb(survivor, NewUser(absorbedId, "absorbed-for-inactive-a@example.com"));
        var mergeToken = SeedMergePending(survivorId, absorbedId);
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.MergeAccountsAsync(
                mergeToken,
                survivorId,
                HostSuppliedClientContext.Empty,
                CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Account is disabled*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAbsorbedInactive_WhenMergeAccountsAsync_ThenStillMergesExternalsAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var microsoftId = SeedProvider("Microsoft");
        var absorbed = NewUser(absorbedId, "absorbed-inactive@example.com");
        absorbed.IsActive = false;
        AddToDb(NewUser(survivorId, "survivor-active@example.com"), absorbed);
        AddToDb(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = absorbedId,
            UserAccount = null!,
            ProviderId = microsoftId,
            ProviderEntity = null!,
            ProviderUserId = "microsoft-on-inactive",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        var mergeToken = SeedMergePending(survivorId, absorbedId);
        var jwt = CreateMergeJwtMock(absorbedId);
        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        var result = await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        result.MovedProviders.Should().BeEquivalentTo("Microsoft");
        (await Context.UsersExternalLogins.AnyAsync(x => x.UserAccountId == survivorId && x.ProviderId == microsoftId))
            .Should().BeTrue();
        (await Context.UsersAccounts.SingleAsync(x => x.Id == absorbedId)).IsActive.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenDuplicateProviderOnSurvivor_WhenMergeAccountsAsync_ThenDropsAbsorbedDuplicateAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var googleId = SeedProvider("Google");
        var microsoftId = SeedProvider("Microsoft");
        AddToDb(NewUser(survivorId, "survivor-dup@example.com"), NewUser(absorbedId, "absorbed-dup@example.com"));
        AddToDb(
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = survivorId,
                UserAccount = null!,
                ProviderId = googleId,
                ProviderEntity = null!,
                ProviderUserId = "google-on-survivor",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = absorbedId,
                UserAccount = null!,
                ProviderId = googleId,
                ProviderEntity = null!,
                ProviderUserId = "google-on-absorbed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new UserExternalLoginEntity
            {
                Id = Guid.NewGuid(),
                UserAccountId = absorbedId,
                UserAccount = null!,
                ProviderId = microsoftId,
                ProviderEntity = null!,
                ProviderUserId = "microsoft-on-absorbed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        var mergeToken = SeedMergePending(survivorId, absorbedId);
        var jwt = CreateMergeJwtMock(absorbedId);
        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        var result = await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        result.MovedProviders.Should().BeEquivalentTo("Microsoft");
        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == survivorId)).Should().Be(2);
        (await Context.UsersExternalLogins.CountAsync(x => x.ProviderId == googleId)).Should().Be(1);
        (await Context.UsersExternalLogins.SingleAsync(x => x.ProviderId == googleId)).ProviderUserId
            .Should().Be("google-on-survivor");
        (await Context.UsersExternalLogins.CountAsync(x => x.UserAccountId == absorbedId)).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenCommunicationEndpoints_WhenMergeAccountsAsync_ThenMovesDistinctDropsDuplicatesAndKeepsOnePreferredAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var microsoftId = SeedProvider("Microsoft");
        AddToDb(NewUser(survivorId, "survivor-ep@example.com"), NewUser(absorbedId, "absorbed-ep@example.com"));
        AddToDb(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = absorbedId,
            UserAccount = null!,
            ProviderId = microsoftId,
            ProviderEntity = null!,
            ProviderUserId = "microsoft-ep",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        // Same key on both → drop absorbed duplicate; distinct SMS preferred on absorbed → move without preferred
        // because survivor already has a preferred email.
        SeedEndpoint(survivorId, ChannelEnum.Email, "shared@example.com", isPreferred: true);
        SeedEndpoint(absorbedId, ChannelEnum.Email, "shared@example.com", isPreferred: false);
        SeedEndpoint(absorbedId, ChannelEnum.Sms, "+15551234567", isPreferred: true);

        var mergeToken = SeedMergePending(survivorId, absorbedId);
        var jwt = CreateMergeJwtMock(absorbedId);
        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        var survivorEndpoints = await Context.UsersCommunicationEndpoints
            .Where(x => x.UserAccountId == survivorId)
            .ToListAsync();
        survivorEndpoints.Should().HaveCount(2);
        survivorEndpoints.Should().ContainSingle(x => x.Channel == ChannelEnum.Email && x.Address == "shared@example.com" && x.IsPreferred);
        survivorEndpoints.Should().ContainSingle(x => x.Channel == ChannelEnum.Sms && x.Address == "+15551234567" && !x.IsPreferred);
        (await Context.UsersCommunicationEndpoints.CountAsync(x => x.UserAccountId == absorbedId)).Should().Be(0);
        survivorEndpoints.Count(x => x.IsPreferred).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAbsorbedPreferredEndpointAndSurvivorWithout_WhenMergeAccountsAsync_ThenPreferredMovesAsync()
    {
        var survivorId = Guid.NewGuid();
        var absorbedId = Guid.NewGuid();
        var microsoftId = SeedProvider("Microsoft");
        AddToDb(NewUser(survivorId, "survivor-pref@example.com"), NewUser(absorbedId, "absorbed-pref@example.com"));
        AddToDb(new UserExternalLoginEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = absorbedId,
            UserAccount = null!,
            ProviderId = microsoftId,
            ProviderEntity = null!,
            ProviderUserId = "microsoft-pref",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        SeedEndpoint(absorbedId, ChannelEnum.Email, "only-on-b@example.com", isPreferred: true);

        var mergeToken = SeedMergePending(survivorId, absorbedId);
        var jwt = CreateMergeJwtMock(absorbedId);
        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);

        await sut.MergeAccountsAsync(
            mergeToken,
            survivorId,
            HostSuppliedClientContext.Empty,
            CancellationToken.None);

        var moved = await Context.UsersCommunicationEndpoints.SingleAsync(x => x.UserAccountId == survivorId);
        moved.Address.Should().Be("only-on-b@example.com");
        moved.IsPreferred.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenLinkStateWithMissingUserAccount_WhenCompleteAsync_ThenThrowsNotFoundExceptionAsync()
    {
        // State row can still hold a UserAccountId that no longer exists (e.g. deleted after mint without FK clear).
        var missingUserId = Guid.NewGuid();
        var payload = new ExternalLoginStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Provider = "Google",
            ReturnUrl = "/account/ExternalLogins",
            UserAccountId = missingUserId,
        };
        var state = EncodeState(payload);
        SeedOAuthState(payload);
        SeedProvider("Google");
        SetAuthenticatedUser(missingUserId);
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Current user account was not found*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingExternalLogin_WhenCompleteAsync_ThenUpdatesExternalLoginAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            DisplayName = "Old Name",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        var externalLogin = await Context.UsersExternalLogins.SingleAsync();
        externalLogin.DisplayName.Should().Be("Google User");
        externalLogin.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMicrosoftProvider_WhenCompleteAsync_ThenUsesMicrosoftProfileEndpointAsync()
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
            ["https://graph.microsoft.com/oidc/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "ms-user-1",
                    email = "ms@example.com",
                    email_verified = true,
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

        completion.UserAccountId.Should().NotBe(Guid.Empty);
        (await Context.UsersExternalLogins.SingleAsync()).ProviderUserId.Should().Be("ms-user-1");
        (await Context.UsersAccounts.SingleAsync()).EmailVerified.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMicrosoftGraphMailWithoutEmailVerified_WhenCompleteAsync_ThenCreatesUnverifiedAccountAsync()
    {
        SeedProvider("Microsoft");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://login.microsoftonline.com/common/oauth2/v2.0/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "ms-token" }),
            ["https://graph.microsoft.com/v1.0/me"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "ms-user-2",
                    mail = "ms-unverified@example.com",
                    displayName = "MS User",
                }),
            ["https://graph.microsoft.com/oidc/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "ms-user-2",
                    email = "ms-unverified@example.com",
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

        var account = await Context.UsersAccounts.SingleAsync(x => x.Id == completion.UserAccountId);
        account.Email.Should().Be("ms-unverified@example.com");
        account.EmailVerified.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMicrosoftEmailVerifiedWithoutOidcEmail_WhenCompleteAsync_ThenGraphMailRemainsUnverifiedAsync()
    {
        SeedProvider("Microsoft");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://login.microsoftonline.com/common/oauth2/v2.0/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "ms-token" }),
            ["https://graph.microsoft.com/v1.0/me"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "ms-user-graph-only",
                    mail = "graph-only@example.com",
                    displayName = "MS Graph Only",
                }),
            ["https://graph.microsoft.com/oidc/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "ms-user-graph-only",
                    email_verified = true,
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

        var account = await Context.UsersAccounts.SingleAsync(x => x.Id == completion.UserAccountId);
        account.Email.Should().Be("graph-only@example.com");
        account.EmailVerified.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenVerifiedEmailAccount_WhenMicrosoftWithoutEmailVerified_ThenDoesNotLinkAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Microsoft");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "ms@example.com",
            UserName = "existing",
            NormalizedUserName = "existing",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://login.microsoftonline.com/common/oauth2/v2.0/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "ms-token" }),
            ["https://graph.microsoft.com/v1.0/me"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "ms-user-3",
                    mail = "ms@example.com",
                    displayName = "MS User",
                }),
            ["https://graph.microsoft.com/oidc/userinfo"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    sub = "ms-user-3",
                    email = "ms@example.com",
                    email_verified = false,
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

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*email already exists*");

        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenGitHubWithoutEmail_WhenCompleteAsync_ThenFetchesPrimaryEmailAsync()
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
                      {"email":"secondary@example.com","primary":false,"verified":true},
                      {"email":"primary@example.com","primary":true,"verified":true}
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
        completion.UserAccountId.Should().NotBe(Guid.Empty);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenGitHubPrimaryEmailUnverified_WhenCompleteAsync_ThenUsesVerifiedFallbackEmailAsync()
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
                }),
            ["https://api.github.com/user/emails"] = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    [
                      {"email":"unverified-primary@example.com","primary":true,"verified":false},
                      {"email":"verified-secondary@example.com","primary":false,"verified":true}
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

        (await Context.UsersAccounts.SingleAsync()).Email.Should().Be("verified-secondary@example.com");
        completion.UserAccountId.Should().NotBe(Guid.Empty);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenFailedTokenExchange_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingAccessTokenInResponse_WhenCompleteAsync_ThenThrowsInvalidOperationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingProviderUserId_WhenCompleteAsync_ThenThrowsInvalidOperationExceptionAsync()
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
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleProvider_WhenInitiateAsync_ThenReturnsAuthorizationUrlAsync()
    {
        SeedProvider("Apple");
        var sut = CreateService(
            GoogleSuccessHandler(),
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", "/home", null, CancellationToken.None);

        url.Should().StartWith("https://appleid.apple.com/auth/authorize?");
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query);
        query["client_id"].Should().Be("com.example.app");
        query["response_mode"].Should().Be("form_post");
        query["scope"].Should().Be("name email");
        query["redirect_uri"].Should().Be(FormCallbackUrl);
        query["state"].Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleFormCallbackUrl_WhenCompleteAsync_ThenTokenExchangeUsesFormCallbackUrlAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001234.formcallback01",
            email = "apple.form@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });

        string? tokenRedirectUri = null;
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = request =>
            {
                tokenRedirectUri = ReadFormRedirectUri(request);
                return OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                });
            },
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var authorizeRedirectUri = System.Web.HttpUtility.ParseQueryString(new Uri(url).Query)["redirect_uri"];
        var state = ExtractState(url);

        await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        authorizeRedirectUri.Should().Be(FormCallbackUrl);
        tokenRedirectUri.Should().Be(FormCallbackUrl);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleSuccessFlow_WhenCompleteAsync_ThenCreatesUserAndExternalLoginAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001234.abcdef0123456789",
            email = "apple.user@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var appleUserJson = """{"name":{"firstName":"Ada","lastName":"Lovelace"},"email":"apple.user@example.com"}""";
        var completion = await sut.CompleteAsync(
            "auth-code",
            state,
            null,
            null,
            CancellationToken.None,
            appleUserJson);

        var user = await Context.UsersAccounts.SingleAsync();
        user.Email.Should().Be("apple.user@example.com");
        user.EmailVerified.Should().BeTrue();

        var login = await Context.UsersExternalLogins.SingleAsync();
        login.ProviderUserId.Should().Be("001234.abcdef0123456789");
        login.ProviderEmail.Should().Be("apple.user@example.com");
        login.DisplayName.Should().Be("Ada Lovelace");
        completion.UserAccountId.Should().Be(user.Id);
        completion.IsLinking.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleReloginWithoutUserJson_WhenCompleteAsync_ThenKeepsSavedDisplayNameAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001333.cccccccccccccccc",
            email = "relogin@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var firstUrl = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        await sut.CompleteAsync(
            "auth-code",
            ExtractState(firstUrl),
            null,
            null,
            CancellationToken.None,
            """{"name":{"firstName":"Ada","lastName":"Lovelace"},"email":"relogin@example.com"}""");

        (await Context.UsersExternalLogins.SingleAsync()).DisplayName.Should().Be("Ada Lovelace");

        // Subsequent Apple callbacks omit form_post `user` — profile.DisplayName is null.
        var secondUrl = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        await sut.CompleteAsync("auth-code", ExtractState(secondUrl), null, null, CancellationToken.None);

        (await Context.UsersExternalLogins.SingleAsync()).DisplayName.Should().Be("Ada Lovelace");
        (await Context.UsersAccounts.CountAsync()).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleWithoutUserJson_WhenCompleteAsync_ThenDisplayNameRemainsNullAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001111.aaaaaaaaaaaaaaaa",
            email = "relay@privaterelay.appleid.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        (await Context.UsersExternalLogins.SingleAsync()).DisplayName.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleNonObjectUserJson_WhenCompleteAsync_ThenIgnoresPayloadAndCompletesAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001222.bbbbbbbbbbbbbbbb",
            email = "array-user@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        // Valid JSON array must not throw via TryGetProperty on a non-object root.
        var completion = await sut.CompleteAsync(
            "auth-code",
            state,
            null,
            null,
            CancellationToken.None,
            "[]");

        var login = await Context.UsersExternalLogins.SingleAsync();
        login.ProviderUserId.Should().Be("001222.bbbbbbbbbbbbbbbb");
        login.DisplayName.Should().BeNull();
        completion.UserAccountId.Should().Be(login.UserAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleUserJsonWithEmptyNames_WhenCompleteAsync_ThenDisplayNameRemainsNullAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001333.cccccccccccccccc",
            email = "empty-name@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        await sut.CompleteAsync(
            "auth-code",
            ExtractState(url),
            null,
            null,
            CancellationToken.None,
            """{"name":{"firstName":"","lastName":"   "}}""");

        (await Context.UsersExternalLogins.SingleAsync()).DisplayName.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleMalformedUserJson_WhenCompleteAsync_ThenIgnoresAndCompletesAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "001444.dddddddddddddddd",
            email = "bad-json@example.com",
            email_verified = "true",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var completion = await sut.CompleteAsync(
            "auth-code",
            ExtractState(url),
            null,
            null,
            CancellationToken.None,
            "{not-json");

        completion.UserAccountId.Should().NotBe(Guid.Empty);
        (await Context.UsersExternalLogins.SingleAsync()).DisplayName.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleWithoutEmail_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Apple");
        var idToken = CreateUnsignedJwt(new
        {
            sub = "009876.fedcba9876543210",
            iss = "https://appleid.apple.com",
            aud = "com.example.app",
        });
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    access_token = "apple-access",
                    id_token = idToken,
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*email*");
        (await Context.UsersAccounts.CountAsync()).Should().Be(0);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAppleMissingIdToken_WhenCompleteAsync_ThenThrowsInvalidOperationExceptionAsync()
    {
        SeedProvider("Apple");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://appleid.apple.com/auth/token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "apple-access" }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "com.example.app",
                    ClientSecret = "apple-client-secret-jwt",
                };
            });

        var url = await sut.InitiateAsync("Apple", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*id_token*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenFacebookProvider_WhenInitiateAsync_ThenReturnsAuthorizationUrlAsync()
    {
        SeedProvider("Facebook");
        var sut = CreateService(
            GoogleSuccessHandler(),
            options =>
            {
                options.Providers["Facebook"] = new ExternalLoginProviderOptions
                {
                    ClientId = "fb-client",
                    ClientSecret = "fb-secret",
                };
            });

        var url = await sut.InitiateAsync("Facebook", "/home", null, CancellationToken.None);

        url.Should().StartWith("https://www.facebook.com/v21.0/dialog/oauth?");
        url.Should().Contain("client_id=fb-client");
        url.Should().Contain("email");
        url.Should().Contain("public_profile");
        url.Should().Contain("redirect_uri=");
        url.Should().Contain("state=");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenFacebookSuccessFlow_WhenCompleteAsync_ThenCreatesUserAndExternalLoginAsync()
    {
        SeedProvider("Facebook");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://graph.facebook.com/v21.0/oauth/access_token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "fb-token" }),
            ["https://graph.facebook.com/v21.0/me"] = request =>
            {
                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
                var proof = query["appsecret_proof"];
                proof.Should().NotBeNullOrWhiteSpace();
                proof.Should().Be(ComputeFacebookAppSecretProof("fb-token", "fb-secret"));
                request.RequestUri!.AbsoluteUri.Should().NotContain("fb-secret");

                return OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "1029384756",
                    name = "Facebook User",
                    email = "fb.user@example.com",
                    picture = new
                    {
                        data = new
                        {
                            url = "https://platform-lookaside.fbsbx.com/avatar.png",
                        },
                    },
                });
            },
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Facebook"] = new ExternalLoginProviderOptions
                {
                    ClientId = "fb-client",
                    ClientSecret = "fb-secret",
                };
            });

        var url = await sut.InitiateAsync("Facebook", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var completion = await sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        var user = await Context.UsersAccounts.SingleAsync();
        user.Email.Should().Be("fb.user@example.com");
        user.EmailVerified.Should().BeFalse();

        var login = await Context.UsersExternalLogins.SingleAsync();
        login.ProviderUserId.Should().Be("1029384756");
        login.ProviderEmail.Should().Be("fb.user@example.com");
        login.DisplayName.Should().Be("Facebook User");
        login.AvatarUrl.Should().Be("https://platform-lookaside.fbsbx.com/avatar.png");
        completion.UserAccountId.Should().Be(user.Id);
        completion.IsLinking.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenFacebookWithoutEmail_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Facebook");
        var handler = new OAuthTestHttpHandler(new Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>>
        {
            ["https://graph.facebook.com/v21.0/oauth/access_token"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new { access_token = "fb-token" }),
            ["https://graph.facebook.com/v21.0/me"] = _ =>
                OAuthTestHttpHandler.JsonResponse(HttpStatusCode.OK, new
                {
                    id = "5566778899",
                    name = "No Email User",
                }),
        });

        var sut = CreateService(
            handler,
            options =>
            {
                options.Providers["Facebook"] = new ExternalLoginProviderOptions
                {
                    ClientId = "fb-client",
                    ClientSecret = "fb-secret",
                };
            });

        var url = await sut.InitiateAsync("Facebook", null, null, CancellationToken.None);
        var state = ExtractState(url);

        var act = () => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*email*");
        (await Context.UsersAccounts.CountAsync()).Should().Be(0);
        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMalformedBase64State_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.CompleteAsync("code", "!!!", null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*OAuth state is invalid*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenDifferentProviderUserIdAlreadyLinked_WhenCompleteAsync_ThenThrowsValidationExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "other-google-id",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });

        var sut = CreateService(GoogleSuccessHandler());
        var url = await sut.InitiateAsync("Google", null, null, CancellationToken.None);
        var state = ExtractState(url);

        await FluentActions.Invoking(() => sut.CompleteAsync("auth-code", state, null, null, CancellationToken.None))
            .Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked to the current user*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenLinkedProviderWithPassword_WhenUnlinkAsync_ThenRemovesLoginRotatesStampAndRevokesTokensAsync()
    {
        var userAccountId = Guid.NewGuid();
        var oldStamp = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            PasswordPhc = "$pbkdf2$has-password",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = oldStamp,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userAccountId);

        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(j => j.RevokeAllTokensForUserAsync(
                userAccountId, RefreshTokenRevokedReason.EXTERNAL_LOGIN_REMOVED, It.IsAny<HostSuppliedClientContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateService(GoogleSuccessHandler(), jwtTokenService: jwt.Object);
        await sut.UnlinkAsync("Google", userAccountId, HostSuppliedClientContext.Empty, CancellationToken.None);

        (await Context.UsersExternalLogins.CountAsync()).Should().Be(0);
        var user = await Context.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        user.SecurityStamp.Should().NotBeNull();
        user.SecurityStamp.Should().NotBe(oldStamp);
        jwt.Verify(
            j => j.RevokeAllTokensForUserAsync(
                userAccountId, RefreshTokenRevokedReason.EXTERNAL_LOGIN_REMOVED, It.IsAny<HostSuppliedClientContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptyUserId_WhenUnlinkAsync_ThenThrowsValidationExceptionAsync()
    {
        SeedProvider("Google");
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.UnlinkAsync("Google", Guid.Empty, HostSuppliedClientContext.Empty, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*UserAccountId is required*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenOAuthOnlyUser_WhenUnlinkAsync_ThenThrowsValidationExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "oauth-only@example.com",
            UserName = "oauth-only",
            NormalizedUserName = "oauth-only",
            PasswordPhc = null,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = providerId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userAccountId);

        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.UnlinkAsync("Google", userAccountId, HostSuppliedClientContext.Empty, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*last login method*");

        (await Context.UsersExternalLogins.CountAsync()).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnlinkedProvider_WhenUnlinkAsync_ThenThrowsNotFoundAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "user@example.com",
            UserName = "user",
            NormalizedUserName = "user",
            PasswordPhc = "$pbkdf2$x",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userAccountId);

        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.UnlinkAsync("Google", userAccountId, HostSuppliedClientContext.Empty, CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("*not linked*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAuthenticatedUser_WhenGetAllAsync_ThenReturnsLinkedAndConfiguredProvidersAsync()
    {
        var userAccountId = Guid.NewGuid();
        var googleId = SeedProvider("Google");
        SeedProvider("Microsoft");
        SeedProvider("GitHub");
        SeedProvider("Apple");
        AddToDb(new UserAccountEntity
        {
            IsActive = true,
            Id = userAccountId,
            Email = "owner@example.com",
            UserName = "owner",
            NormalizedUserName = "owner",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserExternalLoginEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            ProviderId = googleId,
            ProviderEntity = null!,
            ProviderUserId = "google-sub-1",
            ProviderEmail = "linked@gmail.com",
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
        SetAuthenticatedUser(userAccountId);

        var sut = CreateService(
            GoogleSuccessHandler(),
            options =>
            {
                options.Providers["Microsoft"] = new ExternalLoginProviderOptions
                {
                    ClientId = "ms-id",
                    ClientSecret = "ms-secret",
                };
                options.Providers["Apple"] = new ExternalLoginProviderOptions
                {
                    ClientId = "apple-id",
                    ClientSecret = "apple-secret",
                    IsEnabled = false,
                };
            });

        var result = await sut.GetAllAsync(userAccountId, CancellationToken.None);

        result.AccountEmail.Should().Be("owner@example.com");
        result.Providers.Should().HaveCount(2);
        result.Providers.Should().NotContain(x => x.Provider == "GitHub");
        result.Providers.Should().NotContain(x => x.Provider == "Apple");

        var google = result.Providers.Single(x => x.Provider == "Google");
        google.IsConnected.Should().BeTrue();
        google.ProviderEmail.Should().Be("linked@gmail.com");
        google.AvatarUrl.Should().Be("https://example.com/avatar.png");

        var microsoft = result.Providers.Single(x => x.Provider == "Microsoft");
        microsoft.IsConnected.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptyUserId_WhenGetAllAsync_ThenThrowsValidationExceptionAsync()
    {
        var sut = CreateService(GoogleSuccessHandler());

        await FluentActions.Invoking(() => sut.GetAllAsync(Guid.Empty, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*UserAccountId is required*");
    }

    private ExternalLoginService CreateService(
        HttpMessageHandler handler,
        Action<ExternalLoginOptions>? configure = null,
        IExternalLoginUserProvisioner? provisioner = null,
        IJwtTokenService? jwtTokenService = null)
    {
        var options = new ExternalLoginOptions
        {
            CallbackUrl = CallbackUrl,
            FormCallbackUrl = FormCallbackUrl,
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

        var audit = new AuditService(Context);
        return new ExternalLoginService(
            Context,
            CreateIdentityContextFactory(),
            httpClientFactory.Object,
            optionsMock.Object,
            _logger.Object,
            jwtTokenService ?? CreateJwtTokenService(),
            new CommunicationEndpointService(Context, audit, TestAuthOptions.Snapshot()),
            audit,
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
            UserAccountId = payload.UserAccountId,
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

    private string SeedMergePending(Guid survivorId, Guid absorbedId, DateTime? expiresAt = null)
    {
        var mergeToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        AddToDb(new AccountMergePendingEntity
        {
            Id = Guid.NewGuid(),
            MergeToken = mergeToken,
            SurvivorUserAccountId = survivorId,
            AbsorbedUserAccountId = absorbedId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        return mergeToken;
    }

    private void SeedEndpoint(
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        bool isPreferred)
    {
        AddToDb(new UserCommunicationEndpointEntity
        {
            Id = Guid.NewGuid(),
            UserAccountId = userAccountId,
            UserAccount = null!,
            Source = CommunicationEndpointSource.Manual,
            Channel = channel,
            Address = address,
            IsVerified = true,
            IsPreferred = isPreferred,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid(),
        });
    }

    private static Mock<IJwtTokenService> CreateMergeJwtMock(Guid absorbedId)
    {
        var jwt = new Mock<IJwtTokenService>();
        jwt.Setup(x => x.RevokeAllTokensForUserAsync(
                absorbedId,
                RefreshTokenRevokedReason.ACCOUNT_MERGED,
                It.IsAny<HostSuppliedClientContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return jwt;
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
                    email_verified = true,
                    name = "Google User",
                    picture = "https://example.com/avatar.png",
                }),
        });

    private static string CreateUnsignedJwt(object payload)
    {
        static string Base64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Base64Url(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        var body = Base64Url(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        return $"{header}.{body}.sig";
    }

    private static string ComputeFacebookAppSecretProof(string accessToken, string appSecret)
    {
        var hash = System.Security.Cryptography.HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(appSecret),
            Encoding.UTF8.GetBytes(accessToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? ReadFormRedirectUri(HttpRequestMessage request)
    {
        var body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
        if (string.IsNullOrEmpty(body))
        {
            return null;
        }

        return System.Web.HttpUtility.ParseQueryString(body)["redirect_uri"];
    }

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
