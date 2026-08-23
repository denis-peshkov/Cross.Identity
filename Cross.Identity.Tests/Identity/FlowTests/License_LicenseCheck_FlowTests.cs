namespace Cross.Identity.Tests.Identity.FlowTests;

[TestFixture]
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

        AddRegistryStep<CollectFormStepFactory>();
        AddRegistryStep<GetUserAccountIdStepFactory>();
        AddRegistryStep<CollectResultStepFactory>();

        RegisterToServiceProvider<IProcessDefinitionProvider, IProcessDefinitionProvider>(_processDefinitionProvider);
        RegisterToServiceProvider<IUserService, IUserService>(CreateUserService());

        AddToDb(new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            Email = Email,
        });
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenNoLicenseKey_WhenExecuteGetUserAccountIdFlow_ThenCompletesAsync()
    {
        IsLicenseGateChecked().Should().BeFalse();

        var result = await ExecuteGetUserAccountIdFlowAsync();

        result.Data.Should().NotBeNull();
        var payload = result.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
        payload.Should().ContainKey("user_account_id");
        IsLicenseGateChecked().Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenInvalidLicenseKey_WhenExecuteGetUserAccountIdFlow_ThenStillCompletesAsync()
    {
        RegisterLicenseAccessor(new IdentityServiceConfiguration { LicenseKey = "aaa.bbb.ccc" });
        LicenseCheckExtensions.ResetLicenseCheckForTests();

        var result = await ExecuteGetUserAccountIdFlowAsync();

        result.Data.Should().NotBeNull();
        IsLicenseGateChecked().Should().BeFalse();
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenSecondFlowCall_WhenExecuteGetUserAccountIdFlow_ThenDoesNotResetLicenseGateAsync()
    {
        await ExecuteGetUserAccountIdFlowAsync();
        IsLicenseGateChecked().Should().BeFalse();

        var second = await ExecuteGetUserAccountIdFlowAsync();

        second.Data.Should().NotBeNull();
        IsLicenseGateChecked().Should().BeFalse();
    }

    private Task<FlowResult> ExecuteGetUserAccountIdFlowAsync()
    {
        return _flowExecutor.ExecuteAsync(
            new Dictionary<string, object?> { ["Email"] = Email },
            Flow,
            FlowOperationEnum.GetUserAccountId,
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
