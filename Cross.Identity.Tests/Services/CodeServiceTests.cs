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
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
        _codeService = new CodeService(Context, _logger.Object, _emailService.Object, _smsService.Object, configuration);
    }

    [Test]
    public async Task SendAsync_ShouldSendEmailForEmailChannel()
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
    public async Task SendAsync_ShouldSendSmsForSmsChannel()
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
    public async Task VerifyAsync_ShouldReturnTrue()
    {
        // Arrange — текущая реализация проверяет запись в БД
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "test@example.com",
            NormalizedEmail = "test@example.com",
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            NormalizedEmail = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await _codeService.VerifyAsync("email", "test@example.com", "123456", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenEmailCodeNotFound()
    {
        var result = await _codeService.VerifyAsync("email", "nobody@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenEmailCodeExpired()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "test@example.com", NormalizedEmail = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            NormalizedEmail = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        var result = await _codeService.VerifyAsync("email", "test@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnTrue_ForPhoneChannel()
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

        var result = await _codeService.VerifyAsync("phone", phone, "123456", CancellationToken.None);
        result.Should().BeTrue();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenPhoneCodeNotFound()
    {
        var result = await _codeService.VerifyAsync("phone", "+9999999999", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenPhoneCodeExpired()
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
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync("phone", phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenPhoneCodeMismatch()
    {
        var userId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userId,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("correct"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync("phone", phone, "wrongcode", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_WhenPhoneMaxAttemptsExceeded()
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
            Attempts = 3,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync("phone", phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    public async Task VerifyAsync_ShouldReturnFalse_ForUnsupportedChannel()
    {
        var result = await _codeService.VerifyAsync("telegram", "user", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

}
