namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class License_Token_FlowTests : RunFlowCommandHandlerTestsBase
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
        AddRegistryStep<TokenStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        // Configure service provider to return requested services
        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(
            headersContextAccessor);
        // Mock IUserService to controllably return successful authentication
        var userServiceMock = new Mock<IUserService>();
        var userId = Guid.NewGuid();
        userServiceMock
            .Setup(s => s.ValidatePasswordAsync("Email", "test@example.com", "P@ssw0rd!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        userServiceMock
            .Setup(s => s.ValidateCodeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        userServiceMock
            .Setup(s => s.GetUserByAsync("Email", "test@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserAccountEntity
            {
                Id = userId,
                Email = "test@example.com",
            });
        RegisterToServiceProvider<IUserService, IUserService>(userServiceMock.Object);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:DeveloperMode"] = "true"
            })
            .Build();
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                configuration));

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
        // Mock IJwtTokenService too so we do not hit the database for RefreshToken
        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock
            .Setup(j => j.AccessTokenExpiresInSeconds)
            .Returns(600);
        jwtMock
            .Setup(j => j.GenerateAccessTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<List<string>>(),
                It.IsAny<List<System.Security.Claims.Claim>>()))
            .ReturnsAsync("access-token");
        jwtMock
            .Setup(j => j.GenerateRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<List<System.Security.Claims.Claim>>()))
            .ReturnsAsync("refresh-token");
        RegisterToServiceProvider<IJwtTokenService, IJwtTokenService>(jwtMock.Object);

        // In this test the database does not participate in token validation, so mocks are sufficient
    }

    [Test]
    public async Task Handle_LicenseRegistration_SuccessfulExecution()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Password"] = "P@ssw0rd!",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Token, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();

        // verify that collectResult returned tokens in OAuth2 format
        var dict = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        dict.Should().ContainKeys("access_token", "refresh_token", "token_type", "expires_in");
        dict["access_token"].Should().NotBeNull();
        dict["refresh_token"].Should().NotBeNull();
        dict["token_type"].Should().Be("Bearer");

        // verify GetService<T>() calls
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IJwtTokenService)), Times.Exactly(1));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Once);
        // (optional) verify total number of invocations
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(6);
    }

    [Test]
    public async Task Handle_InvalidInput_ShouldThrowValidationException()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["Password"] = "123", // password too short
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Token, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // verify that an error message is present
    }
}
