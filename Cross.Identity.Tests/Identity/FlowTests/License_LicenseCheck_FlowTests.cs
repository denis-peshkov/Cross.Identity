namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
[Category(TestCategory.INTEGRATION)]
internal class License_LicenseCheck_FlowTests : RunFlowCommandHandlerTestsBase
{
    private const string Flow = "main";
    private const string Email = "license-check@example.com";

    [SetUp]
    public override void Setup()
    {
        base.Setup();

        Initialize();
        LicenseCheckExtensions.ResetLicenseCheckForTests();

        var headersContextAccessor = new HeadersContextAccessor
        {
            LanguageCode = "EN",
            CurrencyCode = "USD",
            UserAgent = "TestAgent",
        };

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<GetUserIdStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IHeadersContextAccessor, IHeadersContextAccessor>(headersContextAccessor);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService(headersContextAccessor));

        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = Email,
        });
    }

    [Test]
    public async Task ExecuteAsync_WithoutLicenseKey_ShouldCompleteFlow()
    {
        IsLicenseGateChecked().Should().BeFalse();

        var result = await ExecuteGetUserIdFlowAsync();

        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("user_id");
        IsLicenseGateChecked().Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WithInvalidLicenseKey_ShouldStillCompleteFlow()
    {
        RegisterLicenseAccessor(new IdentityServiceConfiguration { LicenseKey = "aaa.bbb.ccc" });
        LicenseCheckExtensions.ResetLicenseCheckForTests();

        var result = await ExecuteGetUserIdFlowAsync();

        result.Data.Should().NotBeNull();
        IsLicenseGateChecked().Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_SecondCall_ShouldNotResetLicenseGate()
    {
        await ExecuteGetUserIdFlowAsync();
        IsLicenseGateChecked().Should().BeFalse();

        var second = await ExecuteGetUserIdFlowAsync();

        second.Data.Should().NotBeNull();
        IsLicenseGateChecked().Should().BeFalse();
    }

    private Task<FlowResult> ExecuteGetUserIdFlowAsync()
    {
        return _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = Email },
            Flow,
            FlowOperationEnum.GetUserId,
            CancellationToken.None);
    }

    private void RegisterLicenseAccessor(IdentityServiceConfiguration configuration)
    {
        var loggerFactory = (ILoggerFactory)_serviceProviderMock.Object.GetService(typeof(ILoggerFactory))!;
        RegisterToServiceProvider<LicenseAccessor, LicenseAccessor>(new LicenseAccessor(configuration, loggerFactory));
    }

    private static bool IsLicenseGateChecked()
    {
        var field = typeof(LicenseCheckExtensions).GetField(
            "_licenseChecked",
            BindingFlags.NonPublic | BindingFlags.Static);

        return field is not null && (bool)field.GetValue(null)!;
    }
}
