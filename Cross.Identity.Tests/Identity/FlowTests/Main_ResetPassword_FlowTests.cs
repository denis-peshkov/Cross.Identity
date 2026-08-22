namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_ResetPassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string Email = "test@example.com";
    private const string ValidCode = "ABCD1234";
    private const string Password = "P@ssw0rd!";

    private Mock<IUserService> _userServiceMock = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<VerifyCodeStepFactory>();
        AddRegistryStep<ResetPasswordStepFactory>();

        _userServiceMock = new Mock<IUserService>();
        _userServiceMock
            .Setup(s => s.GetUserIdByAsync("Email", Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());
        _userServiceMock
            .Setup(s => s.SetPasswordAsync("Email", Email, Password, ClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(_userServiceMock.Object);
        RegisterToServiceProvider<IEmailSenderService, IEmailSenderService>(Mock.Of<IEmailSenderService>());
        RegisterToServiceProvider<ISmsSenderService, ISmsSenderService>(Mock.Of<ISmsSenderService>());

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true",
            })
            .Build();
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                configuration));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        RegisterToServiceProvider<IHttpContextAccessor, IHttpContextAccessor>(httpContextAccessor.Object);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenValidCode_WhenResetPasswordFlow_ThenPassesPasswordToUserServiceAsync()
    {
        SeedEmailCode(ValidCode, expiresAt: DateTime.UtcNow.AddMinutes(10));

        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Code"] = ValidCode,
            ["Password"] = Password,
        };

        await _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None);

        _userServiceMock.Verify(
            s => s.SetPasswordAsync("Email", Email, Password, ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenAlreadyUsedCode_WhenResetPasswordFlow_ThenRejectsBeforePasswordChangeAsync()
    {
        SeedEmailCode(ValidCode, expiresAt: DateTime.UtcNow.AddMinutes(10), usedAt: DateTime.UtcNow.AddMinutes(-1));

        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Code"] = ValidCode,
            ["Password"] = Password,
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired verification code*");

        _userServiceMock.Verify(
            s => s.SetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenReusedCode_WhenResetPasswordFlowSecondAttempt_ThenRejectsAsync()
    {
        SeedEmailCode(ValidCode, expiresAt: DateTime.UtcNow.AddMinutes(10));

        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Code"] = ValidCode,
            ["Password"] = Password,
        };

        await _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None);

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired verification code*");

        _userServiceMock.Verify(
            s => s.SetPasswordAsync("Email", Email, Password, ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenMissingCode_WhenResetPasswordFlow_ThenThrowsValidationExceptionAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Password"] = Password,
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>();

        _userServiceMock.Verify(
            s => s.SetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidCode_WhenResetPasswordFlow_ThenRejectsBeforePasswordChangeAsync()
    {
        SeedEmailCode(ValidCode, expiresAt: DateTime.UtcNow.AddMinutes(10));

        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Code"] = "WRONGCOD",
            ["Password"] = Password,
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired verification code*");

        _userServiceMock.Verify(
            s => s.SetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenExpiredCode_WhenResetPasswordFlow_ThenRejectsBeforePasswordChangeAsync()
    {
        SeedEmailCode(ValidCode, expiresAt: DateTime.UtcNow.AddMinutes(-5));

        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Code"] = ValidCode,
            ["Password"] = Password,
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Invalid or expired verification code*");

        _userServiceMock.Verify(
            s => s.SetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmbeddedResetPasswordDefinition_WhenParsed_ThenRequiresCodeVerificationBeforeReset()
    {
        var json = _processDefinitionProvider.GetJson(Flow, FlowOperationEnum.ResetPassword);
        using var doc = JsonDocument.Parse(json);
        var steps = doc.RootElement.GetProperty("steps");

        string? passwordKey = null;
        string? collectNext = null;
        string? verifyNext = null;
        var formFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var codeRequired = false;
        var hasVerifyCode = false;

        foreach (var step in steps.EnumerateArray())
        {
            var kind = step.GetProperty("kind").GetString();
            if (string.Equals(kind, "collectForm", StringComparison.OrdinalIgnoreCase))
            {
                collectNext = step.GetProperty("next").GetString();
                foreach (var field in step.GetProperty("schemaDef").GetProperty("fields").EnumerateArray())
                {
                    var key = field.GetProperty("key").GetString()!;
                    formFieldKeys.Add(key);
                    if (string.Equals(key, "Code", StringComparison.OrdinalIgnoreCase))
                        codeRequired = field.GetProperty("required").GetBoolean();
                }
            }

            if (string.Equals(kind, "verifyCode", StringComparison.OrdinalIgnoreCase))
            {
                hasVerifyCode = true;
                verifyNext = step.GetProperty("next").GetString();
            }

            if (string.Equals(kind, "resetPassword", StringComparison.OrdinalIgnoreCase))
            {
                passwordKey = step.GetProperty("passwordKey").GetString();
            }
        }

        passwordKey.Should().Be("collectForm.Password");
        formFieldKeys.Should().Contain("Password");
        formFieldKeys.Should().Contain("Code");
        formFieldKeys.Should().NotContain("NewPassword");
        formFieldKeys.Should().NotContain("OldPassword");
        codeRequired.Should().BeTrue();
        hasVerifyCode.Should().BeTrue();
        collectNext.Should().Be("verifyCode");
        verifyNext.Should().Be("resetPassword");
    }

    private void SeedEmailCode(string code, DateTime expiresAt, DateTime? usedAt = null)
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = Email.ToLowerInvariant(),
            EmailConfirmed = true,
            IsActive = true,
        });
        AddToDb(new EmailVerificationEntity
        {
            UserAccountId = userId,
            UserAccount = null!,
            Email = Email.ToLowerInvariant(),
            TokenHash = CodeGeneratorHelper.GenerateHash(code),
            TokenLength = (byte)code.Length,
            Attempts = 0,
            MaxAttempts = 3,
            ExpiresAt = expiresAt,
            UsedAt = usedAt,
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
        });
    }
}
