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
        // Disable send rate limits by default so resend / multi-send unit tests stay focused.
        _codeService = new CodeService(
            Context,
            _logger.Object,
            _emailService.Object,
            _smsService.Object,
            configuration,
            TestAuthOptions.Snapshot(new AuthenticationOptions
            {
                OtpSendRateLimit = new AuthenticationOptions.OtpSendRateLimitOptions
                {
                    Cooldown = TimeSpan.Zero,
                    MaxSendsPerWindow = 0,
                },
            }));
    }

    private CodeService CreateCodeServiceWithRateLimit(AuthenticationOptions.OtpSendRateLimitOptions limits)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
        return new CodeService(
            Context,
            _logger.Object,
            _emailService.Object,
            _smsService.Object,
            configuration,
            TestAuthOptions.Snapshot(new AuthenticationOptions
            {
                OtpSendRateLimit = limits,
            }));
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
        var userAccountId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        _emailService.Setup(s => s.SendAsync("", "test@example.com", "Test", "Test body", "<html>Test body</html>", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _codeService.SendAsync(message, "123456", userAccountId, ttl, CancellationToken.None);

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
        var userAccountId = Guid.NewGuid();

        _smsService.Setup(s => s.SendAsync("+1234567890", "Your code is 123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync("sms-id");

        // Act
        await _codeService.SendAsync(message, "123456", userAccountId, ttl, CancellationToken.None);

        // Assert
        _smsService.Verify(s => s.SendAsync("+1234567890", "Your code is 123456", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMessengerChannel_WhenSendAsync_ThenThrowsNotSupportedExceptionAsync()
    {
        var message = NotificationMessage.For(ChannelEnum.Telegram, "chat-id")
            .WithTextBody("Your code is 123456");
        var ttl = TimeSpan.FromMinutes(5);
        var userAccountId = Guid.NewGuid();

        await FluentActions.Invoking(() =>
                _codeService.SendAsync(message, "123456", userAccountId, ttl, CancellationToken.None))
            .Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported*");

        _emailService.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _smsService.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Context.EmailVerifications.Should().BeEmpty();
        Context.PhoneVerifications.Should().BeEmpty();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSmsSendAndVerify_WhenDestinationTrimmedOnly_ThenMatchesStoredPhoneAsync()
    {
        var userAccountId = Guid.NewGuid();
        var storedPhone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = storedPhone, IsActive = true });
        var message = NotificationMessage.For(ChannelEnum.Sms, "  +1234567890  ")
            .WithTextBody("Your code is 123456");
        var ttl = TimeSpan.FromMinutes(5);

        _smsService.Setup(s => s.SendAsync(storedPhone, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("sms-id");

        await _codeService.SendAsync(message, "123456", userAccountId, ttl, CancellationToken.None);

        (await Context.PhoneVerifications.SingleAsync()).PhoneNumber.Should().Be(storedPhone);

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, "  +1234567890  ", "123456", CancellationToken.None);

        result.Should().BeTrue();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailCode_WhenVerifyAsync_ThenReturnsTrueAsync()
    {
        // Arrange — current implementation checks the database record
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "test@example.com",
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
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
        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        var stored = await Context.EmailVerifications.SingleAsync();
        stored.UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidEmailCodeWithWhitespace_WhenVerifyAsync_ThenTrimsAndReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userAccountId,
            Email = "test@example.com",
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(
            userAccountId, ChannelEnum.Email, "test@example.com", "  123456  ", CancellationToken.None);

        result.Should().BeTrue();
        (await Context.EmailVerifications.SingleAsync()).UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUsedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
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

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPreviouslyVerifiedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeTrue();
        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(Guid.NewGuid(), ChannelEnum.Email, "nobody@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidPhoneCode_WhenVerifyAsync_ThenReturnsTrueAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone });
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

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeTrue();
        var stored = await Context.PhoneVerifications.SingleAsync();
        stored.UsedAt.Should().NotBeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUsedPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone });
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
            UsedAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(Guid.NewGuid(), ChannelEnum.Sms, "+9999999999", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("123456"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMismatchedPhoneCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("correct"),
            CodeLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, phone, "wrongcode", CancellationToken.None);
        result.Should().BeFalse();
        (await Context.PhoneVerifications.SingleAsync()).Attempts.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenPhoneMaxAttemptsExceeded_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var phone = "+1234567890";
        AddToDb(new UserAccountEntity { Id = userAccountId, PhoneNumber = phone });
        AddToDb(new PhoneVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            PhoneNumber = phone,
            CodeHash = CodeGeneratorHelper.GenerateHash("123456"),
            CodeLength = 6,
            Attempts = 3,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Sms, phone, "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMismatchedEmailCode_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("correct"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "wrongcode", CancellationToken.None);
        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenEmailMaxAttemptsExceeded_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 3,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenWrongIdentity_WhenVerifyAsync_ThenDoesNotIncrementAttemptsAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "victim@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "victim@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "attacker@example.com", "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenRepeatedWrongEmailCode_WhenVerifyAsync_ThenBlocksAfterMaxAttemptsAsync()
    {
        var userAccountId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = "test@example.com" });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userAccountId,
            UserAccount = null!,
            Email = "test@example.com",
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "wrong1", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "wrong2", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "wrong3", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, "test@example.com", "123456", CancellationToken.None)).Should().BeFalse();

        var stored = await Context.EmailVerifications.SingleAsync();
        stored.Attempts.Should().Be(3);
        stored.UsedAt.Should().BeNull();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenResentEmailCode_WhenVerifyAsyncWithPreviousCode_ThenReturnsFalseAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "test@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email });
        var ttl = TimeSpan.FromMinutes(5);
        var message = NotificationMessage.For(ChannelEnum.Email, email)
            .WithSubject("Test")
            .WithTextBody("body");

        await _codeService.SendAsync(message, "OLD-CODE", userAccountId, ttl, CancellationToken.None);
        await _codeService.SendAsync(message, "NEW-CODE", userAccountId, ttl, CancellationToken.None);

        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, email, "OLD-CODE", CancellationToken.None)).Should().BeFalse();
        (await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, email, "NEW-CODE", CancellationToken.None)).Should().BeTrue();

        (await Context.EmailVerifications.CountAsync(x => x.UsedAt != null)).Should().Be(1);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenCooldown_WhenSendAsyncTwice_ThenSecondThrowsValidationExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "cooldown@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email });
        var sut = CreateCodeServiceWithRateLimit(new AuthenticationOptions.OtpSendRateLimitOptions
        {
            Cooldown = TimeSpan.FromMinutes(1),
            MaxSendsPerWindow = 0,
        });
        var message = NotificationMessage.For(ChannelEnum.Email, email)
            .WithSubject("Test")
            .WithTextBody("body");

        await sut.SendAsync(message, "111111", userAccountId, TimeSpan.FromMinutes(5), CancellationToken.None);

        await FluentActions.Invoking(() =>
                sut.SendAsync(message, "222222", userAccountId, TimeSpan.FromMinutes(5), CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*wait*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenWindowCap_WhenSendAsyncExceedsMax_ThenThrowsValidationExceptionAsync()
    {
        var userAccountId = Guid.NewGuid();
        var email = "window@example.com";
        AddToDb(new UserAccountEntity { Id = userAccountId, Email = email });
        var sut = CreateCodeServiceWithRateLimit(new AuthenticationOptions.OtpSendRateLimitOptions
        {
            Cooldown = TimeSpan.Zero,
            MaxSendsPerWindow = 2,
            Window = TimeSpan.FromHours(1),
        });
        var message = NotificationMessage.For(ChannelEnum.Email, email)
            .WithSubject("Test")
            .WithTextBody("body");
        var ttl = TimeSpan.FromMinutes(5);

        await sut.SendAsync(message, "111111", userAccountId, ttl, CancellationToken.None);
        await sut.SendAsync(message, "222222", userAccountId, ttl, CancellationToken.None);

        await FluentActions.Invoking(() =>
                sut.SendAsync(message, "333333", userAccountId, ttl, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*Too many*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInactiveUser_WhenVerifyAsync_ThenReturnsFalseWithoutIncrementingAttemptsAsync()
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

        var result = await _codeService.VerifyAsync(userAccountId, ChannelEnum.Email, email, "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenUnsupportedChannel_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var result = await _codeService.VerifyAsync(Guid.NewGuid(), ChannelEnum.Telegram, "user", "123456", CancellationToken.None);
        result.Should().BeFalse();
    }


    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenCodeOwnedByOtherUser_WhenVerifyAsync_ThenReturnsFalseAsync()
    {
        var victimId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var email = "shared@example.com";
        AddToDb(new UserAccountEntity { Id = victimId, Email = email, IsActive = true });
        AddToDb(new UserAccountEntity { Id = attackerId, Email = email, IsActive = true });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = victimId,
            UserAccount = null!,
            Email = email,
            TokenHash = CodeGeneratorHelper.GenerateHash("123456"),
            TokenLength = 6,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow,
        });

        var result = await _codeService.VerifyAsync(attackerId, ChannelEnum.Email, email, "123456", CancellationToken.None);

        result.Should().BeFalse();
        (await Context.EmailVerifications.SingleAsync()).UsedAt.Should().BeNull();
        (await Context.EmailVerifications.SingleAsync()).Attempts.Should().Be(0);
    }
}
