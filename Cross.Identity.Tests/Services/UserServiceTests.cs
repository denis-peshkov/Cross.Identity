namespace Cross.Identity.Tests.Services;

[TestFixture]
public class UserServiceTests : EFTestsBase
{
    private Mock<ILogger<UserService>> _logger = null!;
    private Mock<IPepperVaultProvider> _pepperVault = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
    private Mock<IOptionsSnapshot<AuthenticationOptions>> _options = null!;
    private CommunicationEndpointService _communicationEndpoints = null!;
    private UserService _userService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _logger = new Mock<ILogger<UserService>>();
        _pepperVault = new Mock<IPepperVaultProvider>();
        _hasher = new Mock<IPasswordHasher>();
        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)1);
        string? pepperValue = "test-pepper";
        _pepperVault.Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny)).Returns((out string v) =>
        {
            v = pepperValue!;
            return true;
        });
        _jwtTokenService = new Mock<IJwtTokenService>();
        _jwtTokenService
            .Setup(j => j.RevokeAllTokensForUserAsync(
                It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), It.IsAny<HostSuppliedClientContext>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _options = CreateOptionsSnapshot();
        _communicationEndpoints = new CommunicationEndpointService(Context, new AuditService(Context), TestAuthOptions.Snapshot());

        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            _communicationEndpoints,
            _communicationEndpoints,
            _options.Object);
    }

    private static Mock<IOptionsSnapshot<AuthenticationOptions>> CreateOptionsSnapshot(
        int maxFailedAccessAttempts = 5,
        TimeSpan? lockoutTimeout = null,
        bool lockoutEnabled = true)
    {
        var mock = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        mock.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Lockout = new AuthenticationOptions.LockoutOptions
            {
                LockoutEnabled = lockoutEnabled,
                MaxFailedAccessAttempts = maxFailedAccessAttempts,
                LockoutTimeout = lockoutTimeout ?? TimeSpan.FromMinutes(15),
            },
        });
        return mock;
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenNewUserDetails_WhenCreateUserAsync_ThenCreatesUserAsync()
    {
        // Arrange
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var hashedPassword = "$pbkdf2-test-hash";

        _hasher.Setup(h => h.Hash(password, It.IsAny<string>())).Returns(hashedPassword);

        var map = new Dictionary<string, object?>
        {
            ["Email"] = email,
            ["Password"] = password
        };

        // Act
        var userAccountId = await _userService.CreateUserAsync(map, CancellationToken.None);

        // Assert
        userAccountId.Should().NotBe(Guid.Empty);
        var user = await Context.UsersAccounts.FirstOrDefaultAsync(u => u.Id == userAccountId);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordPhc.Should().Be(hashedPassword);
        user.LockoutEnabled.Should().BeTrue();
        user.AccessFailedCount.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenNonStringUserName_WhenCreateUserAsync_ThenUserNameMatchesNormalizedSourceAsync()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>(), It.IsAny<string>())).Returns("$pbkdf2-test-hash");

        var map = new Dictionary<string, object?>
        {
            ["UserName"] = new StringBuilder("Alice"),
            ["Password"] = "P@ssw0rd!",
        };

        var userAccountId = await _userService.CreateUserAsync(map, CancellationToken.None);

        var user = await Context.UsersAccounts.FindAsync(userAccountId);
        user!.UserName.Should().Be("Alice");
        user.NormalizedUserName.Should().Be("alice");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenNonStringPhoneNumber_WhenCreateUserAsync_ThenPhoneNumberIsStoredAsync()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>(), It.IsAny<string>())).Returns("$pbkdf2-test-hash");

        var map = new Dictionary<string, object?>
        {
            ["PhoneNumber"] = new StringBuilder("+12125551234"),
            ["Password"] = "P@ssw0rd!",
        };

        var userAccountId = await _userService.CreateUserAsync(map, CancellationToken.None);

        var user = await Context.UsersAccounts.FindAsync(userAccountId);
        user!.PhoneNumber.Should().Be("+12125551234");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenVerifiedEmail_WhenCreateUserAsync_ThenThrowsConflictExceptionAsync()
    {
        var email = "existing@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailVerified = true,
        });

        var map = new Dictionary<string, object?>
        {
            ["Email"] = email,
            ["Password"] = "P@ssw0rd!"
        };

        await FluentActions.Invoking(() => _userService.CreateUserAsync(map, CancellationToken.None))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("*Email already exists*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedEmailSquat_WhenCreateUserAsync_ThenAllowsRegistrationAsync()
    {
        var email = "victim@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            EmailVerified = false,
        });

        var map = new Dictionary<string, object?>
        {
            ["Email"] = email,
            ["Password"] = "P@ssw0rd!"
        };

        var userAccountId = await _userService.CreateUserAsync(map, CancellationToken.None);

        userAccountId.Should().NotBe(Guid.Empty);
        (await Context.UsersAccounts.CountAsync(x => x.Email == email)).Should().Be(2);
        (await Context.UsersAccounts.CountAsync(x => x.Email == email && x.EmailVerified)).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenVerifiedPhone_WhenCreateUserAsync_ThenThrowsConflictExceptionAsync()
    {
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phone,
            PhoneNumberVerified = true,
        });

        var map = new Dictionary<string, object?>
        {
            ["PhoneNumber"] = phone,
            ["Password"] = "P@ssw0rd!"
        };

        await FluentActions.Invoking(() => _userService.CreateUserAsync(map, CancellationToken.None))
            .Should()
            .ThrowAsync<ConflictException>()
            .WithMessage("*PhoneNumber already exists*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedPhoneSquat_WhenCreateUserAsync_ThenAllowsRegistrationAsync()
    {
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phone,
            PhoneNumberVerified = false,
        });

        var map = new Dictionary<string, object?>
        {
            ["PhoneNumber"] = phone,
            ["Password"] = "P@ssw0rd!"
        };

        var userAccountId = await _userService.CreateUserAsync(map, CancellationToken.None);

        userAccountId.Should().NotBe(Guid.Empty);
        (await Context.UsersAccounts.CountAsync(x => x.PhoneNumber == phone)).Should().Be(2);
        (await Context.UsersAccounts.CountAsync(x => x.PhoneNumber == phone && x.PhoneNumberVerified)).Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserById_WhenGetUserAccountIdByAsync_ThenReturnsUserIdAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "test@example.com",
        });

        var result = await _userService.GetUserAccountIdByAsync("Id", userAccountId.ToString(), CancellationToken.None);

        result.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserId_WhenGetUserAccountIdByAsync_ThenReturnsUserIdAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "userid-alias@example.com",
        });

        var result = await _userService.GetUserAccountIdByAsync("UserAccountId", userAccountId.ToString(), CancellationToken.None);

        result.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByEmail_WhenGetUserAccountIdByAsync_ThenReturnsUserIdAsync()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
        });

        // Act
        var result = await _userService.GetUserAccountIdByAsync("Email", email, CancellationToken.None);

        // Assert
        result.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingUserByEmail_WhenGetUserAccountIdByAsync_ThenReturnsNullAsync()
    {
        var result = await _userService.GetUserAccountIdByAsync("Email", "nonexistent@example.com", CancellationToken.None);

        result.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserName_WhenGetUserAccountIdByAsync_ThenReturnsUserIdAsync()
    {
        var userAccountId = Guid.NewGuid();
        var userName = "testuser";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "test@example.com",
        });

        var result = await _userService.GetUserAccountIdByAsync("UserName", userName, CancellationToken.None);

        result.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByPhone_WhenGetUserAccountIdByAsync_ThenReturnsUserIdAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+12125551234";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            PhoneNumber = phone,
            Email = "test@example.com",
        });
        var result = await _userService.GetUserAccountIdByAsync("PhoneNumber", phone, CancellationToken.None);

        result.Should().Be(userAccountId);
    }


    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedSelector_WhenGetUserAccountIdByAsync_ThenThrowsNotSupportedExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.GetUserAccountIdByAsync("Unknown", "value", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByEmail_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        // Arrange
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            UserName = "testuser",
            NormalizedUserName = "testuser"
        });

        // Act
        var result = await _userService.GetUserByAsync("Email", email, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userAccountId);
        result.Email.Should().Be(email);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedSquatAndVerifiedEmail_WhenGetUserByAsync_ThenPrefersVerifiedAsync()
    {
        var email = "shared@example.com";
        var squatId = Guid.NewGuid();
        var verifiedId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = squatId,
            Email = email,
            UserName = "squat",
            NormalizedUserName = "squat",
            EmailVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserAccountEntity
        {
            Id = verifiedId,
            Email = email,
            UserName = "owner",
            NormalizedUserName = "owner",
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var result = await _userService.GetUserByAsync("Email", email, CancellationToken.None);

        result.Id.Should().Be(verifiedId);
        result.EmailVerified.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnverifiedSquatAndVerifiedPhone_WhenGetUserByAsync_ThenPrefersVerifiedAsync()
    {
        var phone = "+79161234567";
        var squatId = Guid.NewGuid();
        var verifiedId = Guid.NewGuid();

        AddToDb(new UserAccountEntity
        {
            Id = squatId,
            Email = "squat@example.com",
            PhoneNumber = phone,
            UserName = "squat-phone",
            NormalizedUserName = "squat-phone",
            PhoneNumberVerified = false,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        AddToDb(new UserAccountEntity
        {
            Id = verifiedId,
            Email = "owner@example.com",
            PhoneNumber = phone,
            UserName = "owner-phone",
            NormalizedUserName = "owner-phone",
            PhoneNumberVerified = true,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });

        var result = await _userService.GetUserByAsync("PhoneNumber", phone, CancellationToken.None);

        result.Id.Should().Be(verifiedId);
        result.PhoneNumberVerified.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserById_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "test@example.com",
            UserName = "u",
            NormalizedUserName = "u"
        });

        var result = await _userService.GetUserByAsync("Id", userAccountId.ToString(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userAccountId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserName_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        var userAccountId = Guid.NewGuid();
        var userName = "myuser";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "e@e.com"
        });

        var result = await _userService.GetUserByAsync("UserName", userName, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userAccountId);
        result.UserName.Should().Be(userName);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedSelector_WhenGetUserByAsync_ThenThrowsNotSupportedExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.GetUserByAsync("Unknown", "v", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailCode_WhenValidateCodeAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("ABC123"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "ABC123", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMultipleEmailVerifications_WhenValidateCodeAsync_ThenUsesLatestActiveVerificationAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("OLD111"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("NEW222"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "NEW222", CancellationToken.None);

        result.Should().BeTrue();

        var verifications = Context.EmailVerifications
            .Where(x => x.UserAccountId == userAccountId)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        verifications[0].Attempts.Should().Be(0);
        verifications[1].Attempts.Should().Be(0);
        verifications[1].UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenWrongEmailCode_WhenValidateCodeAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email, LockoutEnabled = true });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("RIGHT"),
            TokenLength = 5,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "WRONG", CancellationToken.None);
        result.Should().BeFalse();

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenLockedOutUser_WhenValidateCodeAsync_ThenReturnsFalseEvenWithValidCodeAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "locked-code@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            LockoutEnabled = true,
            AccessFailedCount = 5,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30),
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("ABC123"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "ABC123", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync(x => x.UserAccountId == userAccountId)).UsedAt.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMaxFailedCodeAttempts_WhenValidateCodeAsyncRepeatedly_ThenLocksOutAccountAsync()
    {
        _options = CreateOptionsSnapshot(maxFailedAccessAttempts: 3, lockoutTimeout: TimeSpan.FromMinutes(10));
        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            _communicationEndpoints,
            _communicationEndpoints,
            _options.Object);

        var userAccountId = Guid.NewGuid();
        var email = "code-lockout@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            LockoutEnabled = true,
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("ABC123"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 10,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        for (var i = 0; i < 3; i++)
        {
            (await _userService.ValidateCodeAsync("Email", email, "WRONG", CancellationToken.None)).Should().BeFalse();
        }

        var locked = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEnd.Should().NotBeNull();

        (await _userService.ValidateCodeAsync("Email", email, "ABC123", CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPriorFailedAttempts_WhenValidateCodeAsyncSucceeds_ThenResetsLockoutAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "code-reset-lockout@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            LockoutEnabled = true,
            AccessFailedCount = 2,
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("ABC123"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        (await _userService.ValidateCodeAsync("Email", email, "ABC123", CancellationToken.None)).Should().BeTrue();

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPhoneCode_WhenValidateCodeAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+12125551234";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone, PhoneNumberVerified = true });
        var sms = await _communicationEndpoints.UpsertAsync(
            userAccountId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, isVerified: true);
        await _communicationEndpoints.SetPreferredAsync(userAccountId, sms.Id, HostSuppliedClientContext.Empty);
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("123456"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _userService.ValidateCodeAsync("PhoneNumber", phone, "123456", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmailSelectorAndSmsOtp_WhenValidateCodeAsync_ThenVerifiesPhoneNotEmailAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "login@example.com";
        var phone = "+12125559876";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            EmailVerified = false,
            PhoneNumber = phone,
            PhoneNumberVerified = false,
            IsActive = true,
        });
        await _communicationEndpoints.UpsertAsync(
            userAccountId, ChannelEnum.Email, email, CommunicationEndpointSource.Account, isVerified: false);
        var sms = await _communicationEndpoints.UpsertAsync(
            userAccountId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, isVerified: true);
        await _communicationEndpoints.SetPreferredAsync(userAccountId, sms.Id, HostSuppliedClientContext.Empty);
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("654321"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "654321", CancellationToken.None);

        result.Should().BeTrue();
        var account = await Context.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        account.EmailVerified.Should().BeFalse();
        account.PhoneNumberVerified.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmailSelectorAndPreferredSms_WhenValidateCodeAsync_ThenAcceptsPhoneCodeAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "login@example.com";
        var phone = "+12125559876";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            EmailVerified = true,
            PhoneNumber = phone,
            PhoneNumberVerified = true,
            IsActive = true,
        });
        await _communicationEndpoints.UpsertAsync(
            userAccountId, ChannelEnum.Email, email, CommunicationEndpointSource.Account, isVerified: true);
        var sms = await _communicationEndpoints.UpsertAsync(
            userAccountId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, isVerified: true);
        await _communicationEndpoints.SetPreferredAsync(userAccountId, sms.Id, HostSuppliedClientContext.Empty);
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("654321"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "654321", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPasswordById_WhenValidatePasswordAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var password = "P@ssw0rd!";
        var hashed = "$pbkdf2$";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "test@example.com",
            PasswordPhc = hashed,
            PasswordPepperVersion = 1,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "test-pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, hashed, "test-pepper")).Returns(PasswordVerificationEnum.Success);

        var result = await _userService.ValidatePasswordAsync("Id", userAccountId.ToString(), password, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPassword_WhenValidatePasswordAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var hashed = "$pbkdf2$";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = hashed,
            PasswordPepperVersion = 1
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "test-pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, hashed, "test-pepper")).Returns(PasswordVerificationEnum.Success);

        var result = await _userService.ValidatePasswordAsync("Email", email, password, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidPassword_WhenValidatePasswordAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1,
            LockoutEnabled = true,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Failed);

        var result = await _userService.ValidatePasswordAsync("Email", email, "wrong", CancellationToken.None);
        result.Should().BeFalse();

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(1);
        user.LockoutEnd.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenValidatePasswordAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "inactive@example.com";
        var password = "P@ssw0rd!";
        var hashedPassword = "$pbkdf2-test-hash";

        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = hashedPassword,
            PasswordPepperVersion = 1,
            IsActive = false,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, hashedPassword, It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Success);

        var result = await _userService.ValidatePasswordAsync("Email", email, password, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenValidateCodeAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "inactive@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            IsActive = false,
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _userService.ValidateCodeAsync("Email", email, "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidCodeByUserName_WhenValidateCodeAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var userName = "alice";
        var email = "alice@example.com";
        var communicationEndpoints = new CommunicationEndpointService(Context, new AuditService(Context), TestAuthOptions.Snapshot());
        var userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            communicationEndpoints,
            communicationEndpoints,
            _options.Object);

        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            UserName = userName,
            NormalizedUserName = userName,
            Email = email,
            EmailVerified = true,
            IsActive = true,
        });
        await communicationEndpoints.UpsertAsync(
            userAccountId,
            ChannelEnum.Email,
            email,
            CommunicationEndpointSource.Account,
            isVerified: true);
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("ABC123"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await userService.ValidateCodeAsync("UserName", userName, "ABC123", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingPepperVersion_WhenValidatePasswordAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "hash",
            PasswordPepperVersion = 99
        });
        _pepperVault.Setup(p => p.TryGetValue((short)99, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = null!;
            return false;
        });

        var result = await _userService.ValidatePasswordAsync("Email", email, "pass", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPasswordNeedingRehash_WhenValidatePasswordAsync_ThenUpdatesPasswordPhcAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var oldHash = "$pbkdf2$old";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = oldHash,
            PasswordPepperVersion = 1
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)2);
        _pepperVault.Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny)).Returns((out string p) =>
        {
            p = "new-pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, oldHash, "pepper")).Returns(PasswordVerificationEnum.SuccessRehashNeeded);
        _hasher.Setup(h => h.Hash(password, "new-pepper")).Returns("$pbkdf2$new");

        var result = await _userService.ValidatePasswordAsync("Email", email, password, CancellationToken.None);

        result.Should().BeTrue();
        var user = await Context.UsersAccounts.FindAsync(userAccountId);
        user!.PasswordPhc.Should().Be("$pbkdf2$new");
        user.PasswordPepperVersion.Should().Be((short)2);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPasswordNeedingRehash_WhenValidatePasswordAsyncCancelled_ThenThrowsOperationCanceledAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var oldHash = "$pbkdf2$old";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = oldHash,
            PasswordPepperVersion = 1,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)2);
        _pepperVault.Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny)).Returns((out string p) =>
        {
            p = "new-pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, oldHash, "pepper")).Returns(PasswordVerificationEnum.SuccessRehashNeeded);
        _hasher.Setup(h => h.Hash(password, "new-pepper")).Returns("$pbkdf2$new");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _userService.ValidatePasswordAsync("Email", email, password, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmptySelectorValue_WhenValidatePasswordAsync_ThenReturnsFalseAsync()
    {
        var result = await _userService.ValidatePasswordAsync("Email", "   ", "pass", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedSelector_WhenValidatePasswordAsync_ThenThrowsNotSupportedExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.ValidatePasswordAsync("Unknown", "v", "p", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUser_WhenSetPasswordAsync_ThenUpdatesPasswordHashAndVersionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        var oldStamp = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$old",
            PasswordPepperVersion = 1,
            SecurityStamp = oldStamp,
        });

        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)2);
        _pepperVault.Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny)).Returns((out string p) =>
        {
            p = "new-pepper";
            return true;
        });
        _hasher.Setup(h => h.Hash("newPass", "new-pepper")).Returns("$pbkdf2$new");

        await _userService.SetPasswordAsync("Email", email, "newPass", HostSuppliedClientContext.Empty, CancellationToken.None);

        var user = await Context.UsersAccounts.FindAsync(userAccountId);
        user.Should().NotBeNull();
        user!.PasswordPhc.Should().Be("$pbkdf2$new");
        user.PasswordPepperVersion.Should().Be((short)2);
        user.SecurityStamp.Should().NotBeNull();
        user.SecurityStamp.Should().NotBe(oldStamp);
        _jwtTokenService.Verify(
            j => j.RevokeAllTokensForUserAsync(userAccountId, RefreshTokenRevokedReason.PASSWORD_CHANGED, HostSuppliedClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingUser_WhenSetPasswordAsync_ThenThrowsNotFoundExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.SetPasswordAsync("Email", "missing@example.com", "newPass", HostSuppliedClientContext.Empty, CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenConcurrentUserAccountUpdates_WhenSavingChanges_ThenThrowsDbUpdateConcurrencyExceptionAsync()
    {
        var dbName = $"user-concurrency-{Guid.NewGuid():N}";
        await using var ctx1 = InMemoryDbHelper.CreateContext(dbName);
        await using var ctx2 = InMemoryDbHelper.CreateContext(dbName);

        var userAccountId = Guid.NewGuid();
        ctx1.UsersAccounts.Add(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "concurrency@example.com",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        await ctx1.SaveChangesAsync();

        var user1 = await ctx1.UsersAccounts.SingleAsync(x => x.Id == userAccountId);
        var user2 = await ctx2.UsersAccounts.SingleAsync(x => x.Id == userAccountId);

        user1.EmailVerified = true;
        await ctx1.SaveChangesAsync();

        user2.PhoneNumberVerified = true;
        await FluentActions.Invoking(() => ctx2.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredLockout_WhenValidatePasswordAsyncFailsOnce_ThenResetsFailedCountBeforeIncrementAsync()
    {
        _options = CreateOptionsSnapshot(maxFailedAccessAttempts: 3, lockoutTimeout: TimeSpan.FromMinutes(10));
        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            Mock.Of<ICommunicationEndpointService>(),
            Mock.Of<ICommunicationEndpointUpsertService>(),
            _options.Object);

        var userAccountId = Guid.NewGuid();
        var email = "expired-lockout@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1,
            LockoutEnabled = true,
            AccessFailedCount = 3,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(-5),
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Failed);

        (await _userService.ValidatePasswordAsync("Email", email, "wrong", CancellationToken.None)).Should().BeFalse();

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(1);
        user.LockoutEnd.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMaxFailedAttempts_WhenValidatePasswordAsyncRepeatedly_ThenLocksOutAccountAsync()
    {
        _options = CreateOptionsSnapshot(maxFailedAccessAttempts: 3, lockoutTimeout: TimeSpan.FromMinutes(10));
        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            Mock.Of<ICommunicationEndpointService>(),
            Mock.Of<ICommunicationEndpointUpsertService>(),
            _options.Object);

        var userAccountId = Guid.NewGuid();
        var email = "lockout@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1,
            LockoutEnabled = true,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Failed);

        for (var i = 0; i < 3; i++)
        {
            (await _userService.ValidatePasswordAsync("Email", email, "wrong", CancellationToken.None)).Should().BeFalse();
        }

        var locked = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        locked.AccessFailedCount.Should().Be(3);
        locked.LockoutEnd.Should().NotBeNull();
        locked.LockoutEnd!.Value.Should().BeAfter(DateTimeOffset.UtcNow);

        _hasher.Setup(h => h.Verify("correct", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Success);

        (await _userService.ValidatePasswordAsync("Email", email, "correct", CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPriorFailedAttempts_WhenValidatePasswordAsyncSucceeds_ThenResetsLockoutAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "reset-lockout@example.com";
        var password = "P@ssw0rd!";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1,
            LockoutEnabled = true,
            AccessFailedCount = 2,
        });
        _pepperVault.Setup(p => p.TryGetValue((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationEnum.Success);

        (await _userService.ValidatePasswordAsync("Email", email, password, CancellationToken.None)).Should().BeTrue();

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenLockedOutUser_WhenSetPasswordAsync_ThenClearsLockoutAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "locked@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = email,
            PasswordPhc = "$pbkdf2$old",
            PasswordPepperVersion = 1,
            LockoutEnabled = true,
            AccessFailedCount = 5,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30),
        });
        _pepperVault.Setup(p => p.TryGetCurrentValue(out It.Ref<string>.IsAny)).Returns((out string p) =>
        {
            p = "pepper";
            return true;
        });
        _hasher.Setup(h => h.Hash("newPass", "pepper")).Returns("$pbkdf2$new");

        await _userService.SetPasswordAsync("Email", email, "newPass", HostSuppliedClientContext.Empty, CancellationToken.None);

        var user = await Context.UsersAccounts.SingleAsync(u => u.Id == userAccountId);
        user.AccessFailedCount.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }
}
