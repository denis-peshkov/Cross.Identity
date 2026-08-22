namespace Cross.Identity.Tests.Services;

[TestFixture]
public class CodeServiceTests : EFTestsBase
{
    private Mock<ILogger<CodeService>> _logger = null!;
    private Mock<IEmailSenderService> _emailService = null!;
    private Mock<ISmsSenderService> _smsService = null!;
    private CodeService _codeService = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _logger = new Mock<ILogger<CodeService>>();
        _emailService = new Mock<IEmailSenderService>();
        _smsService = new Mock<ISmsSenderService>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "false"
            })
            .Build();
        _codeService = new CodeService(Context, _logger.Object, _emailService.Object, _smsService.Object, configuration);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmailNotification_WhenSendAsync_ThenSendsEmailAsync()
    {
        // Arrange
        var message = NotificationMessage.For(ChannelEnum.Email, "test@example.com")
            .WithSubject("Test")
            .WithTextBody("Test body")
            .WithTextHtml("<html>Test body</html>");
        var ttl = TimeSpan.FromMinutes(5);
        const string userId = "00000000-0000-0000-0000-000000000001";

        _emailService.Setup(s => s.SendAsync("", "test@example.com", "Test", "Test body", "<html>Test body</html>", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _codeService.SendAsync(message, "123456", userId, ttl, CancellationToken.None);

        // Assert
        _emailService.Verify(s => s.SendAsync("", "test@example.com", "Test", "Test body", "<html>Test body</html>", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSmsNotification_WhenSendAsync_ThenSendsSmsAsync()
    {
        // Arrange
        var message = NotificationMessage.For(ChannelEnum.Sms, "+1234567890")
            .WithTextBody("Your code is 123456");
        var ttl = TimeSpan.FromMinutes(5);
        var userId = Guid.NewGuid().ToString();

        _smsService.Setup(s => s.SendAsync("+1234567890", "Your code is 123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync("sms-id");

        // Act
        await _codeService.SendAsync(message, "123456", userId, ttl, CancellationToken.None);

        // Assert
        _smsService.Verify(s => s.SendAsync("+1234567890", "Your code is 123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailCode_WhenVerifyAsync_ThenReturnsTrueAsync()
    {
        // Arrange — current implementation checks the database record
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "test@example.com",
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var stored = await Context.EmailVerifications.SingleAsync();
        stored.UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUsedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UsedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPreviouslyVerifiedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeTrue();
        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "nobody@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPhoneCode_WhenVerifyAsync_ThenReturnsTrueAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
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

        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeTrue();
        var stored = await Context.PhoneVerifications.SingleAsync();
        stored.UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUsedPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
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
            UsedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, "+9999999999", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMismatchedPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("correct"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, phone, "wrongcode", CancellationToken.None);
        result.Should().BeFalse();
        (await Context.PhoneVerifications.SingleAsync()).Attempts.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPhoneMaxAttemptsExceeded_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("123456"),
            CodeLength = 6,
            Attempts = 3,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMismatchedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("correct"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "wrongcode", CancellationToken.None);
        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmailMaxAttemptsExceeded_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 3,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenWrongIdentity_WhenVerifyAsync_ThenDoesNotIncrementAttemptsAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "victim@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "victim@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, "attacker@example.com", "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRepeatedWrongEmailCode_WhenVerifyAsync_ThenBlocksAfterMaxAttemptsAsync()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "wrong1", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "wrong2", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "wrong3", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeFalse();

        var stored = await Context.EmailVerifications.SingleAsync();
        stored.Attempts.Should().Be(3);
        stored.UsedAt.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenVerifyAsync_ThenReturnsFalseWithoutIncrementingAttemptsAsync()
    {
        var userId = Guid.NewGuid();
        var email = "inactive@example.com";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = email,
            IsActive = false,
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _codeService.VerifyAsync(ChannelEnum.Email, email, "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedChannel_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(ChannelEnum.Telegram, "user", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

}
