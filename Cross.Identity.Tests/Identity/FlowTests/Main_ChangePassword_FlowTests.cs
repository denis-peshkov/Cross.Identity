namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_ChangePassword_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private static readonly Guid UserId = Guid.Parse("c5f92314-65f9-47be-87cd-4ec8f881ae4a");
    private const string CurrentPassword = "CurrentP@ss1";
    private const string NewPassword = "NewP@ssw0rd!";

    private Mock<IUserService> _userServiceMock = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<PasswordAuthStepFactory>();
        AddRegistryStep<ResetPasswordStepFactory>();

        var userIdText = UserId.ToString();
        _userServiceMock = new Mock<IUserService>();
        _userServiceMock
            .Setup(s => s.ValidatePasswordAsync("Id", userIdText, CurrentPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _userServiceMock
            .Setup(s => s.SetPasswordAsync("Id", userIdText, NewPassword, ClientContext.Empty, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userServiceMock
            .Setup(s => s.GetUserIdByAsync("Id", userIdText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserId);
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
    public async Task GivenValidCurrentPassword_WhenChangePasswordFlowRuns_ThenPassesNewPasswordToUserServiceAsync()
    {
        var input = new Dictionary<string, object?>
        {
            ["Id"] = UserId.ToString(),
            ["CurrentPassword"] = CurrentPassword,
            ["NewPassword"] = NewPassword,
        };

        await _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ChangePassword, CancellationToken.None);

        _userServiceMock.Verify(
            s => s.ValidatePasswordAsync("Id", UserId.ToString(), CurrentPassword, It.IsAny<CancellationToken>()),
            Times.Once);
        _userServiceMock.Verify(
            s => s.SetPasswordAsync("Id", UserId.ToString(), NewPassword, ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidCurrentPassword_WhenChangePasswordFlowRuns_ThenRejectsBeforePasswordChangeAsync()
    {
        _userServiceMock
            .Setup(s => s.ValidatePasswordAsync("Id", UserId.ToString(), CurrentPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var input = new Dictionary<string, object?>
        {
            ["Id"] = UserId.ToString(),
            ["CurrentPassword"] = CurrentPassword,
            ["NewPassword"] = NewPassword,
        };

        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, Flow, FlowOperationEnum.ChangePassword, CancellationToken.None))
            .Should()
            .ThrowAsync<NotAuthorizedException>()
            .WithMessage("Invalid credentials.");

        _userServiceMock.Verify(
            s => s.SetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), ClientContext.Empty, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenEmbeddedChangePasswordDefinition_WhenParsed_ThenRequiresCurrentPasswordBeforeReset()
    {
        var json = _processDefinitionProvider.GetJson(Flow, FlowOperationEnum.ChangePassword);
        using var doc = JsonDocument.Parse(json);
        var steps = doc.RootElement.GetProperty("steps");

        string? collectNext = null;
        string? authNext = null;
        string? authPasswordKey = null;
        string? collectSelectorField = null;
        string? resetPasswordKey = null;
        var formFieldKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var step in steps.EnumerateArray())
        {
            var kind = step.GetProperty("kind").GetString();
            if (string.Equals(kind, "collectForm", StringComparison.OrdinalIgnoreCase))
            {
                collectNext = step.GetProperty("next").GetString();
                collectSelectorField = step.GetProperty("selector").GetProperty("candidates")[0].GetString();
                foreach (var field in step.GetProperty("schemaDef").GetProperty("fields").EnumerateArray())
                {
                    formFieldKeys.Add(field.GetProperty("key").GetString()!);
                }
            }

            if (string.Equals(kind, "passwordAuth", StringComparison.OrdinalIgnoreCase))
            {
                authNext = step.GetProperty("next").GetString();
                authPasswordKey = step.GetProperty("passwordKey").GetString();
            }

            if (string.Equals(kind, "resetPassword", StringComparison.OrdinalIgnoreCase))
            {
                resetPasswordKey = step.GetProperty("passwordKey").GetString();
            }
        }

        formFieldKeys.Should().Contain("Id");
        formFieldKeys.Should().Contain("CurrentPassword");
        formFieldKeys.Should().Contain("NewPassword");
        formFieldKeys.Should().NotContain("Email");
        formFieldKeys.Should().NotContain("Code");
        collectNext.Should().Be("passwordAuth");
        authNext.Should().Be("resetPassword");
        collectSelectorField.Should().Be("Id");
        authPasswordKey.Should().Be("collectForm.CurrentPassword");
        resetPasswordKey.Should().Be("collectForm.NewPassword");
    }
}
