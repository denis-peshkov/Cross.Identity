namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class License_ResetPassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "license";
    private const string Email = "test@example.com";
    private const string Password = "P@ssw0rd!";

    private Mock<IUserService> _userServiceMock = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<ResetPasswordStepFactory>();

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };

        _userServiceMock = new Mock<IUserService>();
        _userServiceMock
            .Setup(s => s.SetPasswordAsync("Email", Email, Password, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(headersContextAccessor);
        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(_userServiceMock.Object);
        RegisterToServiceProvider<IEmailSenderService, IEmailSenderService>(Mock.Of<IEmailSenderService>());
        RegisterToServiceProvider<ISmsSenderService, ISmsSenderService>(Mock.Of<ISmsSenderService>());

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.42");
        httpContextAccessor.Setup(x => x.HttpContext).Returns(context);
        RegisterToServiceProvider<IHttpContextAccessor, IHttpContextAccessor>(httpContextAccessor.Object);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task ResetPassword_WithPassword_ShouldPassPasswordToUserService()
    {
        var input = new Dictionary<string, object?>
        {
            ["Email"] = Email,
            ["Password"] = Password,
        };

        await _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ResetPassword, CancellationToken.None);

        _userServiceMock.Verify(
            s => s.SetPasswordAsync("Email", Email, Password, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void ResetPassword_EmbeddedDefinition_PasswordKeyShouldMatchCollectFormField()
    {
        var json = _processDefinitionProvider.GetJson(Flow, FlowOperationEnum.ResetPassword);
        using var doc = JsonDocument.Parse(json);
        var steps = doc.RootElement.GetProperty("steps");

        string? passwordKey = null;
        var formFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps.EnumerateArray())
        {
            var kind = step.GetProperty("kind").GetString();
            if (string.Equals(kind, "collectForm", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var field in step.GetProperty("schemaDef").GetProperty("fields").EnumerateArray())
                {
                    formFieldKeys.Add(field.GetProperty("key").GetString()!);
                }
            }

            if (string.Equals(kind, "resetPassword", StringComparison.OrdinalIgnoreCase))
            {
                passwordKey = step.GetProperty("passwordKey").GetString();
            }
        }

        passwordKey.Should().NotBeNullOrWhiteSpace();
        passwordKey!.Should().Be("collectForm.Password");
        formFieldKeys.Should().Contain("Password");
        formFieldKeys.Should().NotContain("NewPassword");
        formFieldKeys.Should().NotContain("OldPassword");
    }
}
