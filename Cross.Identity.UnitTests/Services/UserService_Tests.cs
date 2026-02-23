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
}
