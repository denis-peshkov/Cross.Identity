namespace Cross.Identity.UnitTests.Identity.FlowTests;

[TestFixture]
public class License_ForgotPassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private string FLOW => "license";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent"
        };

        // Register step factories
        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<ForgotPasswordStepFactory>();

        // Configure service provider to return requested services
        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(
            headersContextAccessor);
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(
            _processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(
            new UserService(
                Context,
                Mock.Of<ILogger<UserService>>(),
                Mock.Of<IPepperVaultProvider>(),
                Mock.Of<IPasswordHasher>(),
                Mock.Of<IPhoneNormalizer>(),
                headersContextAccessor));
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>()));

        var optionsSnapshot = new Mock<IOptionsSnapshot<AuthenticationOptions>>();
        optionsSnapshot.Setup(o => o.Value).Returns(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Issuer = "http://localhost:5000",
                Audience = "http://localhost:5000",
                Key = "tTPm5yP2Q+1m7UQlM3N2AVnleqk7D4HhR0YzF9o5+Xw=",
                EncryptionKey = "r9lZJcR8CdpqgGgxP1VbUk2OQhlnwFJSwVOrMDyk4Lc=",
                UseEncryption = false,
                AccessTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenExpires = TimeSpan.FromMinutes(10),
                RefreshTokenAbsoluteExpires = TimeSpan.FromDays(30),
            }
        });
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        context.Request.Headers["User-Agent"] = "MyTestUA";
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(
            new JwtTokenService(
                Context,
                optionsSnapshot.Object,
                httpContextAccessor.Object));

        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = "Test@Example.Com",
            NormalizedEmail = "test@example.com",
        });
    }

    [Test]
    public async Task Handle_LicenseRegistration_SuccessfulExecution()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.ForgotPassword, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        // result.Data.ToString().Length.Should().Be(8);
        // проверка вызовов GetService<T>()
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        // (необязательно) проверить суммарное число обращений
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(5);
    }

    [Test]
    public async Task Handle_InvalidInput_ShouldThrowValidationException()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.ForgotPassword, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // проверяем что есть сообщение об ошибке
    }
}
