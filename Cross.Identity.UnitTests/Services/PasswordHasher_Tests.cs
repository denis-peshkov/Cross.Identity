namespace Cross.Identity.UnitTests.Services;

[TestFixture]
public class PasswordHasher_Tests
{
    private CrossIdentityPasswordHasher _hasher = null!;
    private Mock<IOptionsMonitor<CrossIdentityPasswordHasherOptions>> _optionsMonitor = null!;

    [SetUp]
    public void SetUp()
    {
        _optionsMonitor = new Mock<IOptionsMonitor<CrossIdentityPasswordHasherOptions>>();
        var options = new CrossIdentityPasswordHasherOptions
        {
            DefaultAlgorithm = PasswordAlgoEnum.PBKDF2,
            SaltSizeBytes = 16,
            HashOutputBytes = 32,
            Pbkdf2_Iterations = 100000
        };
        _optionsMonitor.Setup(o => o.CurrentValue).Returns(options);
        _hasher = new CrossIdentityPasswordHasher(_optionsMonitor.Object);
    }

    [Test]
    public void Hash_ShouldReturnPhcString()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";

        // Act
        var hash = _hasher.Hash(password, pepper);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().StartWith("$pbkdf2-");
    }

    [Test]
    public void Hash_ShouldProduceDifferentHashForSamePassword()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";

        // Act
        var hash1 = _hasher.Hash(password, pepper);
        var hash2 = _hasher.Hash(password, pepper);

        // Assert - разные соли должны давать разные хеши
        hash1.Should().NotBe(hash2);
    }

    [Test]
    public void Verify_ShouldReturnSuccessForCorrectPassword()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";
        var hash = _hasher.Hash(password, pepper);

        // Act
        var result = _hasher.Verify(password, hash, pepper);

        // Assert
        result.Should().BeOneOf(PasswordVerificationEnum.Success, PasswordVerificationEnum.SuccessRehashNeeded);
    }

    [Test]
    public void Verify_ShouldReturnFailedForIncorrectPassword()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var wrongPassword = "WrongPassword";
        var pepper = "test-pepper";
        var hash = _hasher.Hash(password, pepper);

        // Act
        var result = _hasher.Verify(wrongPassword, hash, pepper);

        // Assert
        result.Should().Be(PasswordVerificationEnum.Failed);
    }

    [Test]
    public void Verify_ShouldReturnFailedForWrongPepper()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";
        var wrongPepper = "wrong-pepper";
        var hash = _hasher.Hash(password, pepper);

        // Act
        var result = _hasher.Verify(password, hash, wrongPepper);

        // Assert
        result.Should().Be(PasswordVerificationEnum.Failed);
    }

    [Test]
    public void NeedsRehash_ShouldReturnFalseForValidHash()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";
        var hash = _hasher.Hash(password, pepper);

        // Act
        var result = _hasher.NeedsRehash(hash);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void NeedsRehash_ShouldReturnTrueForInvalidFormat()
    {
        // Arrange
        var invalidHash = "invalid-hash-format";

        // Act
        var result = _hasher.NeedsRehash(invalidHash);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public void Hash_WithArgon2id_ShouldReturnArgon2PhcString()
    {
        _optionsMonitor.Setup(o => o.CurrentValue).Returns(new CrossIdentityPasswordHasherOptions
        {
            DefaultAlgorithm = PasswordAlgoEnum.Argon2id,
            SaltSizeBytes = 16,
            HashOutputBytes = 32,
            Argon2_Iterations = 2,
            Argon2_MemoryKb = 65536,
            Argon2_DegreeOfParallelism = 1
        });
        _hasher = new CrossIdentityPasswordHasher(_optionsMonitor.Object);

        var hash = _hasher.Hash("P@ssw0rd!", "pepper");
        hash.Should().NotBeNullOrEmpty().And.StartWith("$argon2id$");
    }

    [Test]
    public void Verify_WithArgon2id_ShouldReturnSuccessForCorrectPassword()
    {
        _optionsMonitor.Setup(o => o.CurrentValue).Returns(new CrossIdentityPasswordHasherOptions
        {
            DefaultAlgorithm = PasswordAlgoEnum.Argon2id,
            SaltSizeBytes = 16,
            HashOutputBytes = 32,
            Argon2_Iterations = 2,
            Argon2_MemoryKb = 65536,
            Argon2_DegreeOfParallelism = 1
        });
        _hasher = new CrossIdentityPasswordHasher(_optionsMonitor.Object);

        var password = "P@ssw0rd!";
        var pepper = "pepper";
        var hash = _hasher.Hash(password, pepper);
        var result = _hasher.Verify(password, hash, pepper);
        result.Should().BeOneOf(PasswordVerificationEnum.Success, PasswordVerificationEnum.SuccessRehashNeeded);
    }

    [Test]
    public void Hash_WithSha256_ShouldReturnSha256PhcString()
    {
        _optionsMonitor.Setup(o => o.CurrentValue).Returns(new CrossIdentityPasswordHasherOptions
        {
            DefaultAlgorithm = PasswordAlgoEnum.SHA256,
            SaltSizeBytes = 16,
            HashOutputBytes = 32
        });
        _hasher = new CrossIdentityPasswordHasher(_optionsMonitor.Object);

        var hash = _hasher.Hash("P@ssw0rd!", "pepper");
        hash.Should().NotBeNullOrEmpty().And.StartWith("$sha256$");
    }

    [Test]
    public void Verify_WithSha256_ShouldReturnSuccessForCorrectPassword()
    {
        _optionsMonitor.Setup(o => o.CurrentValue).Returns(new CrossIdentityPasswordHasherOptions
        {
            DefaultAlgorithm = PasswordAlgoEnum.SHA256,
            SaltSizeBytes = 16,
            HashOutputBytes = 32
        });
        _hasher = new CrossIdentityPasswordHasher(_optionsMonitor.Object);

        var password = "P@ssw0rd!";
        var pepper = "pepper";
        var hash = _hasher.Hash(password, pepper);
        var result = _hasher.Verify(password, hash, pepper);
        result.Should().BeOneOf(PasswordVerificationEnum.Success, PasswordVerificationEnum.SuccessRehashNeeded);
    }

    [Test]
    public void Verify_WhenPhcUnknownPrefix_ShouldReturnFailed()
    {
        var result = _hasher.Verify("p", "$unknown$format", "pepper");
        result.Should().Be(PasswordVerificationEnum.Failed);
    }
}
