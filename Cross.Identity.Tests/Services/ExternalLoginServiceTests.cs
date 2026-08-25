namespace Cross.Identity.Tests.Services;

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
        url.Should().Contain("client_id=google-client");
        url.Should().Contain("redirect_uri=");
        url.Should().Contain("app.example%2fcallback");
        url.Should().Contain("response_type=code");
        url.Should().Contain("scope=openid");
        url.Should().Contain("state=");
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
    public async Task GivenMatchingEmailWithoutExternalLogin_WhenCompleteAsync_ThenLinksToExistingUserAsync()
    {
        var userAccountId = Guid.NewGuid();
        SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
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
    public async Task GivenExternalAccountLinkedToAnotherUser_WhenCompleteAsyncForLinking_ThenThrowsValidationExceptionAsync()
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
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });
        AddToDb(new UserExternalLoginEntity
        {
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

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*already linked to another user*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingExternalLogin_WhenCompleteAsync_ThenUpdatesExternalLoginAsync()
    {
        var userAccountId = Guid.NewGuid();
        var providerId = SeedProvider("Google");
        AddToDb(new UserAccountEntity
        {
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
    public async Task GivenAppleProvider_WhenCompleteAsync_ThenThrowsNotSupportedExceptionAsync()
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
            jwtTokenService ?? CreateJwtTokenService(),
            new CommunicationEndpointService(Context, new AuditService(Context), TestAuthOptions.Snapshot()),
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
