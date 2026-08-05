namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
internal class Main_Registration_FlowTests : RunFlowCommandHandlerTestsBase
{
    private string FLOW => "main";

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
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService(headersContextAccessor));
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

        // Load JSON as embedded /ProcessEngine/Definitions/Flows/main.Register.json
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
    public async Task GivenValidRegistrationInput_WhenExecuteRegisterFlow_ThenSucceedsAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "test@example.com",
            ["Password"] = "P@ssw0rd!",
        };

        // Act
        var result = await _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("UserId");
        payload["UserId"].Should().BeOfType<string>().Which.Should().NotBeNullOrWhiteSpace();
        // verify GetService<T>() calls
        _serviceProviderMock.Verify(x => x.GetService(typeof(IServiceScopeFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IFormValidatorFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IRequestInput)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(IUserService)), Times.Exactly(2));
        _serviceProviderMock.Verify(x => x.GetService(typeof(ICodeService)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(ILoggerFactory)), Times.Once);
        _serviceProviderMock.Verify(x => x.GetService(typeof(IHostEnvironment)), Times.Once);
        // (optional) verify total number of invocations
        _serviceProviderMock.Invocations.Count.Should().BeGreaterOrEqualTo(10);
    }

    [Test]
    public async Task GivenInvalidRegistrationInput_WhenExecuteRegisterFlow_ThenThrowsValidationExceptionAsync()
    {
        // Arrange
        var input = new Dictionary<string, object?>
        {
            ["Email"] = "invalid-email",
            ["FullName"] = "J", // name too short
            ["Company"] = "C", // company name too short
            ["Password"] = "123", // password too short
            ["AcceptLicenseTerms"] = false // required field
        };

        // Act & Assert
        await FluentActions.Invoking(() =>
                _flowExecutor.ExecuteAsync(input, FLOW, FlowOperationEnum.Register, CancellationToken.None))
            .Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("*"); // verify that an error message is present
    }
}
