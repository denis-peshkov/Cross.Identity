namespace Cross.Identity.UnitTests.Services;

[TestFixture]
public class CodeService_Tests : EFTestsBase
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
        _codeService = new CodeService(Context, _logger.Object, _emailService.Object, _smsService.Object);
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

        _emailService.Setup(s => s.SendAsync("", "dionis.peshkov@gmail.com", "Test", "Test body", "<html>Test body</html>", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _codeService.SendAsync(message, "123456", userId, ttl, CancellationToken.None);

        // Assert
        _emailService.Verify(s => s.SendAsync("", "dionis.peshkov@gmail.com", "Test", "Test body", "<html>Test body</html>", It.IsAny<CancellationToken>()), Times.Once);
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
            NormalizedEmail = "test@example.com"
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            Email = "test@example.com",
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
}
