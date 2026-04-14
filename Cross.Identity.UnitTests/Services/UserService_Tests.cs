namespace Cross.Identity.UnitTests.Services;

[TestFixture]
public class UserService_Tests : EFTestsBase
{
    private Mock<ILogger<UserService>> _logger = null!;
    private Mock<IPepperVaultProvider> _pepperVault = null!;
    private Mock<IPasswordHasher> _hasher = null!;
    private Mock<IPhoneNormalizer> _phoneNormalizer = null!;
    private Mock<IHeadersContextAccessor> _headersContextAccessor = null!;
    private UserService _userService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _logger = new Mock<ILogger<UserService>>();
        _pepperVault = new Mock<IPepperVaultProvider>();
        _hasher = new Mock<IPasswordHasher>();
        _phoneNormalizer = new Mock<IPhoneNormalizer>();
        _headersContextAccessor = new Mock<IHeadersContextAccessor>();

        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)1);
        string? pepperValue = "test-pepper";
        _pepperVault.Setup(p => p.TryGetCurrentVersion(out It.Ref<string>.IsAny)).Returns((out string v) =>
        {
            v = pepperValue!;
            return true;
        });
        _headersContextAccessor.Setup(h => h.LanguageCode).Returns("US");
        _phoneNormalizer.Setup(p => p.NormalizeToE164(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((p, r) => p);

        _userService = new UserService(
            Context,
            _logger.Object,
            _pepperVault.Object,
            _hasher.Object,
            _phoneNormalizer.Object,
            _headersContextAccessor.Object);
    }

    [Test]
    public async Task CreateUserAsync_ShouldCreateUser()
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
        user!.Email.Should().Be(email);
        user.NormalizedEmail.Should().Be(email.ToLowerInvariant());
        user.PasswordPhc.Should().Be(hashedPassword);
    }

    [Test]
    public async Task CreateUserAsync_ShouldThrowWhenEmailExists()
    {
        // Arrange
        var email = "existing@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToLowerInvariant()
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
    public async Task GetUserIdByAsync_ShouldReturnUserIdForEmail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant()
        });

        // Act
        var result = await _userService.GetUserIdByAsync("Email", email, CancellationToken.None);

        // Assert
        result.Should().Be(userId.ToString());
    }

    [Test]
    public async Task GetUserIdByAsync_ShouldThrowWhenUserNotFound()
    {
        // Act & Assert
        await FluentActions.Invoking(() => _userService.GetUserIdByAsync("Email", "nonexistent@example.com", CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("*not found*");
    }

    [Test]
    public async Task GetUserIdByAsync_ShouldReturnUserIdForUserName()
    {
        var userId = Guid.NewGuid();
        var userName = "testuser";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "test@example.com",
            NormalizedEmail = "test@example.com"
        });

        var result = await _userService.GetUserIdByAsync("UserName", userName, CancellationToken.None);

        result.Should().Be(userId.ToString());
    }

    [Test]
    public async Task GetUserIdByAsync_ShouldReturnUserIdForPhone()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            PhoneNumber = phone,
            Email = "test@example.com",
            NormalizedEmail = "test@example.com"
        });
        _phoneNormalizer.Setup(p => p.NormalizeToE164(phone, "US")).Returns(phone);

        var result = await _userService.GetUserIdByAsync("Phone", phone, CancellationToken.None);

        result.Should().Be(userId.ToString());
    }

    [Test]
    public async Task GetUserIdByAsync_ShouldThrow_WhenSelectorNotSupported()
    {
        await FluentActions.Invoking(() => _userService.GetUserIdByAsync("Unknown", "value", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");
    }

    [Test]
    public async Task GetUserByAsync_ShouldReturnUserDictionary()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
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
    public async Task GetUserByAsync_ShouldReturnUser_WhenSelectorIsId()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "test@example.com",
            NormalizedEmail = "test@example.com",
            UserName = "u",
            NormalizedUserName = "u"
        });

        var result = await _userService.GetUserByAsync("Id", userId.ToString(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
    }

    [Test]
    public async Task GetUserByAsync_ShouldReturnUser_WhenSelectorIsUserName()
    {
        var userId = Guid.NewGuid();
        var userName = "myuser";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToLowerInvariant(),
            Email = "e@e.com",
            NormalizedEmail = "e@e.com"
        });

        var result = await _userService.GetUserByAsync("UserName", userName, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.UserName.Should().Be(userName);
    }

    [Test]
    public async Task GetUserByAsync_ShouldThrow_WhenSelectorNotSupported()
    {
        await FluentActions.Invoking(() => _userService.GetUserByAsync("Unknown", "v", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>();
    }

    [Test]
    public async Task ValidateCodeAsync_ShouldReturnTrue_ForValidEmailCode()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userId, Email = email, NormalizedEmail = email.ToLowerInvariant() });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
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

        var user = Context.UsersAccounts.First(x => x.Id == userId);
        user.EmailConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task ValidateCodeAsync_ShouldReturnFalse_WhenCodeWrong()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userId, Email = email, NormalizedEmail = email.ToLowerInvariant() });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
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
    public async Task ValidateCodeAsync_ShouldReturnTrue_ForValidPhoneCode()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userId,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("123456"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _userService.ValidateCodeAsync("Phone", phone, "123456", CancellationToken.None);
        result.Should().BeTrue();

        var user = Context.UsersAccounts.First(x => x.Id == userId);
        user.PhoneConfirmed.Should().BeTrue();
    }

    [Test]
    public async Task ValidatePasswordAsync_ShouldReturnTrue_WhenPasswordValid()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var hashed = "$pbkdf2$";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            PasswordPhc = hashed,
            PasswordPepperVersion = 1
        });
        _pepperVault.Setup(p => p.TryGet((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "test-pepper";
            return true;
        });
        _hasher.Setup(h => h.Verify(password, hashed, "test-pepper")).Returns(PasswordVerificationEnum.Success);

        var result = await _userService.ValidatePasswordAsync("Email", email, password, CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    public async Task ValidatePasswordAsync_ShouldReturnFalse_WhenPasswordInvalid()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            PasswordPhc = "$pbkdf2$stored",
            PasswordPepperVersion = 1
        });
        _pepperVault.Setup(p => p.TryGet((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
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
    public async Task ValidatePasswordAsync_ShouldReturnFalse_WhenPepperNotFound()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            PasswordPhc = "hash",
            PasswordPepperVersion = 99
        });
        _pepperVault.Setup(p => p.TryGet((short)99, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = null!;
            return false;
        });

        var result = await _userService.ValidatePasswordAsync("Email", email, "pass", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidatePasswordAsync_WhenRehashNeeded_ShouldUpdatePasswordPhc()
    {
        var userId = Guid.NewGuid();
        var email = "test@example.com";
        var password = "P@ssw0rd!";
        var oldHash = "$pbkdf2$old";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            PasswordPhc = oldHash,
            PasswordPepperVersion = 1
        });
        _pepperVault.Setup(p => p.TryGet((short)1, out It.Ref<string>.IsAny)).Returns((short v, out string p) =>
        {
            p = "pepper";
            return true;
        });
        _pepperVault.Setup(p => p.CurrentVersion).Returns((short)2);
        _pepperVault.Setup(p => p.TryGetCurrentVersion(out It.Ref<string>.IsAny)).Returns((out string p) =>
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
    public async Task ValidatePasswordAsync_ShouldReturnFalse_WhenSelectorValueEmpty()
    {
        var result = await _userService.ValidatePasswordAsync("Email", "   ", "pass", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task ValidatePasswordAsync_ShouldThrow_WhenSelectorNotSupported()
    {
        await FluentActions.Invoking(() => _userService.ValidatePasswordAsync("Unknown", "v", "p", CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>();
    }

    [Test]
    public async Task SetPasswordAsync_ShouldThrowNotImplementedException()
    {
        await FluentActions.Invoking(() => _userService.SetPasswordAsync("Email", "test@example.com", "newPass", CancellationToken.None))
            .Should()
            .ThrowAsync<NotImplementedException>();
    }
}
