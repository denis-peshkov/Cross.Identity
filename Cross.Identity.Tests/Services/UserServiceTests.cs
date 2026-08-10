namespace Cross.Identity.Tests.Services;

[TestFixture]
public class UserServiceTests : EFTestsBase
{
    private Mock<ILogger<UserService>> _logger = null!;
    private Mock<IPepperVaultProvider> _pepperVault = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private Mock<IJwtTokenService> _jwtTokenService = null!;
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
                It.IsAny<Guid>(), It.IsAny<RefreshTokenRevokedReason>(), null, null, null, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _jwtTokenService.Object,
            Mock.Of<ICommunicationEndpointService>());
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
        var userId = await _userService.CreateUserAsync(map, CancellationToken.None);

        // Assert
        userId.Should().NotBeNullOrEmpty();
        var user = await Context.UsersAccounts.FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        user.Should().NotBeNull();
        user!.Email.Should().Be(email.ToLowerInvariant());
        user.PasswordPhc.Should().Be(hashedPassword);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingEmail_WhenCreateUserAsync_ThenThrowsInvalidOperationExceptionAsync()
    {
        // Arrange
        var email = "existing@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
        });

        var map = new Dictionary<string, object?>
        {
            ["Email"] = email,
            ["Password"] = "P@ssw0rd!"
        };

        // Act & Assert
        await FluentActions.Invoking(() => _userService.CreateUserAsync(map, CancellationToken.None))
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email already exists*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserById_WhenGetUserIdByAsync_ThenReturnsUserIdAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "test@example.com",
        });

        var result = await _userService.GetUserIdByAsync("Id", userId.ToString(), CancellationToken.None);

        result.Should().Be(userId.ToString());
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserId_WhenGetUserIdByAsync_ThenReturnsUserIdAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "userid-alias@example.com",
        });

        var result = await _userService.GetUserIdByAsync("UserId", userId.ToString(), CancellationToken.None);

        result.Should().Be(userId.ToString());
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByEmail_WhenGetUserIdByAsync_ThenReturnsUserIdAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
        });

        // Act
        var result = await _userService.GetUserIdByAsync("Email", email, CancellationToken.None);

        // Assert
        result.Should().Be(userId.ToString());
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingUserByEmail_WhenGetUserIdByAsync_ThenThrowsNotFoundExceptionAsync()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _userService.GetUserIdByAsync("Email", "nonexistent@example.com", CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserName_WhenGetUserIdByAsync_ThenReturnsUserIdAsync()
    {
        var userId = Guid.NewGuid();
        var userName = "testuser";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "test@example.com",
        });

        var result = await _userService.GetUserIdByAsync("UserName", userName, CancellationToken.None);

        result.Should().Be(userId.ToString());
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByPhone_WhenGetUserIdByAsync_ThenReturnsUserIdAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+12125551234";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            PhoneNumber = phone,
            Email = "test@example.com",
        });
        var result = await _userService.GetUserIdByAsync("PhoneNumber", phone, CancellationToken.None);

        result.Should().Be(userId.ToString());
    }


    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedSelector_WhenGetUserIdByAsync_ThenThrowsNotSupportedExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.GetUserIdByAsync("Unknown", "value", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByEmail_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            UserName = "testuser",
            NormalizedUserName = "testuser"
        });

        // Act
        var result = await _userService.GetUserByAsync("Email", email, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Email.Should().Be(email);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserById_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "u",
            NormalizedUserName = "u"
        });

        var result = await _userService.GetUserByAsync("Id", userId.ToString(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExistingUserByUserName_WhenGetUserByAsync_ThenReturnsUserAsync()
    {
        var userId = Guid.NewGuid();
        var userName = "myuser";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "e@e.com"
        });

        var result = await _userService.GetUserByAsync("UserName", userName, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userId, Email = email });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userId, Email = email });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
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
            UserAccountId = userId,
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
            .Where(x => x.UserAccountId == userId)
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userId, Email = email });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
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
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPhoneCode_WhenValidateCodeAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+12125551234";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userId,
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
    public async Task GivenValidPasswordById_WhenValidatePasswordAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var password = "P@ssw0rd!";
        var hashed = "$pbkdf2$";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
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

        var result = await _userService.ValidatePasswordAsync("Id", userId.ToString(), password, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPassword_WhenValidatePasswordAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var hashed = "$pbkdf2$";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1
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
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingPepperVersion_WhenValidatePasswordAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var oldHash = "$pbkdf2$old";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
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
        var user = await Context.UsersAccounts.FindAsync(userId);
        user!.PasswordPhc.Should().Be("$pbkdf2$new");
        user.PasswordPepperVersion.Should().Be((short)2);
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
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var oldStamp = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
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

        await _userService.SetPasswordAsync("Email", email, "newPass", null, null, null, CancellationToken.None);

        var user = await Context.UsersAccounts.FindAsync(userId);
        user.Should().NotBeNull();
        user!.PasswordPhc.Should().Be("$pbkdf2$new");
        user.PasswordPepperVersion.Should().Be((short)2);
        user.SecurityStamp.Should().NotBeNull();
        user.SecurityStamp.Should().NotBe(oldStamp);
        _jwtTokenService.Verify(
            j => j.RevokeAllTokensForUserAsync(userId, RefreshTokenRevokedReason.PASSWORD_CHANGED, null, null, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingUser_WhenSetPasswordAsync_ThenThrowsNotFoundExceptionAsync()
    {
        await FluentActions.Invoking(() => _userService.SetPasswordAsync("Email", "missing@example.com", "newPass", null, null, null, CancellationToken.None))
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

        var userId = Guid.NewGuid();
        ctx1.UsersAccounts.Add(new UserAccountEntity
        {
            Id = userId,
            Email = "concurrency@example.com",
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        });
        await ctx1.SaveChangesAsync();

        var user1 = await ctx1.UsersAccounts.SingleAsync(x => x.Id == userId);
        var user2 = await ctx2.UsersAccounts.SingleAsync(x => x.Id == userId);

        user1.EmailConfirmed = true;
        await ctx1.SaveChangesAsync();

        user2.PhoneNumberConfirmed = true;
        await FluentActions.Invoking(() => ctx2.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateConcurrencyException>();
    }
}
