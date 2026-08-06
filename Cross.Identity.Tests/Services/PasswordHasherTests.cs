namespace Cross.Identity.Tests.Services;

[TestFixture]
public class PasswordHasherTests
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
    [Category(TestCategory.UNIT)]
    public void GivenPassword_WhenHash_ThenReturnsPhcString()
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
    [Category(TestCategory.UNIT)]
    public void GivenSamePassword_WhenHashTwice_ThenProducesDifferentHashes()
    {
        // Arrange
        var password = "P@ssw0rd!";
        var pepper = "test-pepper";

        // Act
        var hash1 = _hasher.Hash(password, pepper);
        var hash2 = _hasher.Hash(password, pepper);

        // Assert - different salts should produce different hashes
        hash1.Should().NotBe(hash2);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenCorrectPasswordAndHash_WhenVerify_ThenReturnsSuccess()
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
    [Category(TestCategory.UNIT)]
    public void GivenIncorrectPassword_WhenVerify_ThenReturnsFailed()
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
    [Category(TestCategory.UNIT)]
    public void GivenWrongPepper_WhenVerify_ThenReturnsFailed()
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
    [Category(TestCategory.UNIT)]
    public void GivenValidHash_WhenNeedsRehash_ThenReturnsFalse()
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
    [Category(TestCategory.UNIT)]
    public void GivenInvalidHashFormat_WhenNeedsRehash_ThenReturnsTrue()
    {
        // Arrange
        var invalidHash = "invalid-hash-format";

        // Act
        var result = _hasher.NeedsRehash(invalidHash);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenArgon2idAlgorithm_WhenHash_ThenReturnsArgon2PhcString()
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
    [Category(TestCategory.UNIT)]
    public void GivenArgon2idHashAndCorrectPassword_WhenVerify_ThenReturnsSuccess()
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
    [Category(TestCategory.UNIT)]
    public void GivenSha256Algorithm_WhenHash_ThenReturnsSha256PhcString()
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
    [Category(TestCategory.UNIT)]
    public void GivenSha256HashAndCorrectPassword_WhenVerify_ThenReturnsSuccess()
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
    [Category(TestCategory.UNIT)]
    public void GivenUnknownPhcPrefix_WhenVerify_ThenReturnsFailed()
    {
        var result = _hasher.Verify("p", "$unknown$format", "pepper");
        result.Should().Be(PasswordVerificationEnum.Failed);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenMalformedPbkdf2Phc_WhenVerify_ThenIsNotTreatedAsValid()
    {
        AssertMalformedPbkdf2PhcIsRejected("$pbkdf2-sha256$i=notanumber$YmFzZTY0$YmFzZTY0");
        AssertMalformedPbkdf2PhcIsRejected("$pbkdf2-sha256$i=1000$!!!not-base64!!!$YmFzZTY0");
    }

    private void AssertMalformedPbkdf2PhcIsRejected(string phc)
    {
        try
        {
            var result = _hasher.Verify("p", phc, "pepper");
            result.Should().Be(PasswordVerificationEnum.Failed);
        }
        catch (FormatException)
        {
            // Acceptable: with strict PHC parsing (Parse / Base64), an exception may be thrown instead of Failed.
        }
    }
}
