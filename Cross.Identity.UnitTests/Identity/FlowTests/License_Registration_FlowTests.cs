namespace Cross.Identity.UnitTests.Identity.FlowTests;

[TestFixture]
public class License_Registration_FlowTests : RunFlowCommandHandlerTestsBase
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
        AddRegistryStep<CreateUserStepFactory>();
        AddRegistryStep<SendCodeStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

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
        var notificationOptions = new Mock<IOptionsSnapshot<NotificationEmailOptions>>();
        notificationOptions.Setup(o => o.Value).Returns(new NotificationEmailOptions());
        RegisterToServiceProvider<ICodeService, ICodeService>(
            new CodeService(
                Context,
                Mock.Of<ILogger<CodeService>>(),
                Mock.Of<IEmailSenderService>(),
                Mock.Of<ISmsSenderService>(),
                notificationOptions.Object));

        // Берём JSON как embedded /Flows/Definitions/licenses.register.json
        // AddJson("""
        //         {
        //           "start": "collectForm",
        //           "steps": [
        //             {
        //               "kind": "collectForm",
        //               "schemaDef": {
        //                 "fields": [
        //                   { "key": "Email", "type": "Email", "required": true },
        //                   { "key": "FullName", "type": "String", "required": true, "min": 3, "max": 128 },
        //                   { "key": "Company", "type": "String", "required": true, "min": 2, "max": 128 },
        //                   { "key": "Password", "type": "Password", "required": true, "min": 8, "max": 128 },
        //                   { "key": "ConfirmPassword", "type": "Password", "required": true, "min": 8, "max": 128 },
        //                   { "key": "AcceptGetEmails", "type": "Bool", "required": false },
        //                   { "key": "AcceptLicenseTerms", "type": "Bool", "required": true }
        //                 ],
        //                 "validators": [
        //                   { "kind": "equal", "left": "Password", "right": "ConfirmPassword", "message": "Passwords do not match." }
        //                 ]
        //               },
        //               "next": "createUser"
        //             },
        //             {
        //               "kind": "createUser",
        //               "map": {
        //                 "Email": "collectForm.Email",
        //                 "FullName": "collectForm.FullName",
        //                 "Company": "collectForm.Company",
        //                 "AcceptGetEmails": "collectForm.AcceptGetEmails",
        //                 "AcceptLicenseTerms": "collectForm.AcceptLicenseTerms"
        //               },
        //               "selectorKey": "collectForm.Email",
        //               "next": "sendCode"
        //             },
        //             {
        //               "kind": "sendCode",
        //               "channel": "email",
        //               "selectorKey": "createUser.selectorKey",
        //               "resolveBy": { "field": "Email" },
        //               "next": "collectResult"
        //             },
        //             {
        //               "kind": "collectResult",
        //               "map": {
        //                 "LastCode": "sendCode.LastCode"
        //               },
        //               "next": null
        //             }
        //           ]
        //         }
        //         """);
    }

    [Test]
    public async Task Handle_LicenseRegistration_SuccessfulExecution()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Password"] = "P@ssw0rd!",
            ["ConfirmPassword"] = "P@ssw0rd!",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.ToString().Length.Should().Be(68);
        // проверка вызовов GetService<T>()
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ICodeService)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IHostEnvironment)), Times.Once);
        // (необязательно) проверить суммарное число обращений
        _serviceProviderMock.Invocations.Count.Should().Be(10);
    }

    [Test]
    public async Task Handle_InvalidInput_ShouldThrowValidationException()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["FullName"] = "J", // слишком короткое имя
            ["Company"] = "C", // слишком короткое название
            ["Password"] = "123", // слишком короткий пароль
            ["ConfirmPassword"] = "456", // не совпадает
            ["AcceptLicenseTerms"] = false // обязательное поле
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // проверяем что есть сообщение об ошибке
    }

    [Test]
    public async Task CollectForm_Should_Validate_Passwords_Equal()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["FullName"] = "John Tester",
            ["Company"] = "Company Inc",
            ["Password"] = "P@ssw0rd!",
            ["ConfirmPassword"] = "P@ssw0rd!--", // не совпадает
            ["AcceptGetEmails"] = true,
            ["AcceptLicenseTerms"] = true,
        };

        // Act & Assert
        await FluentActions.Invoking(() => _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*Passwords do not match*");
    }
}
