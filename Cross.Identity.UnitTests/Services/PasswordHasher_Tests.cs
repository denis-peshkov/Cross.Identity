namespace Cross.Identity.UnitTests.Services;

using CrossIdentityPasswordHasher = Cross.Identity.Services.Crypto.PasswordHasher;
using CrossIdentityPasswordHasherOptions = Cross.Identity.Services.Crypto.PasswordHasherOptions;

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
}
