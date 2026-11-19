namespace Cross.Identity.Tests.Identity.StepTests;

[TestFixture]
public class CommunicationEndpoints_StepTests
{
    private Mock<ICommunicationEndpointService> _endpoints = null!;
    private Mock<ICodeService> _codeService = null!;

    [SetUp]
    public void SetUp()
    {
        _endpoints = new Mock<ICommunicationEndpointService>();
        _codeService = new Mock<ICodeService>();
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserId_WhenGetAll_ThenWritesEndpointsAsync()
    {
        var userAccountId = Guid.NewGuid();
        var list = new List<CommunicationEndpointDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Channel = ChannelEnum.Email,
                Address = "a@b.co",
                IsVerified = true,
                IsPreferred = true,
                Source = CommunicationEndpointSource.Account,
            },
        };
        _endpoints.Setup(s => s.GetAllAsync(userAccountId, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var step = new CommunicationEndpointsGetAllStep
        {
            Kind = "communicationEndpointsGetAll",
            UserAccountIdKey = "UserAccountId",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointsGetAll.UserAccountId", userAccountId);
        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<IReadOnlyList<CommunicationEndpointDto>>("communicationEndpointsGetAll.Endpoints").Should().BeEquivalentTo(list);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenUserIdAndEndpoint_WhenSetPreferred_ThenPassesHostSuppliedClientContextAsync()
    {
        var userAccountId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        _endpoints
            .Setup(s => s.SetPreferredAsync(userAccountId, endpointId, new HostSuppliedClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var step = new CommunicationEndpointSetPreferredStep
        {
            Kind = "communicationEndpointSetPreferred",
            UserAccountIdKey = "UserAccountId",
            EndpointIdKey = "EndpointId",
            CommunicationEndpoints = _endpoints.Object,
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointSetPreferred.UserAccountId", userAccountId)
            .Set("communicationEndpointSetPreferred.EndpointId", endpointId.ToString())
            .Set("collectForm.IpAddress", "1.2.3.4")
            .Set("collectForm.UserAgent", "ua")
            .Set("collectForm.DeviceFingerprint", "fp");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("communicationEndpointSetPreferred.Preferred").Should().BeTrue();
        _endpoints.Verify(
            s => s.SetPreferredAsync(userAccountId, endpointId, new HostSuppliedClientContext("1.2.3.4", "ua", "fp"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenEmail_WhenAdd_ThenAddAndSendsOtpAsync()
    {
        var userAccountId = Guid.NewGuid();
        var endpoint = new CommunicationEndpointDto
        {
            Id = Guid.NewGuid(),
            Channel = ChannelEnum.Email,
            Address = "second@example.com",
            IsVerified = false,
            Source = CommunicationEndpointSource.Manual,
        };
        _endpoints
            .Setup(s => s.AddAsync(userAccountId, ChannelEnum.Email, "second@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(endpoint);
        _codeService
            .Setup(s => s.SendAsync(
                It.IsAny<NotificationMessage>(),
                It.IsAny<string>(),
                userAccountId,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:ClientUrl"] = "http://localhost:4200",
                ["Authentication:DeveloperMode"] = "true",
            })
            .Build();

        var embeddedOptions = Microsoft.Extensions.Options.Options.Create(new EmbeddedProcessDefinitionOptions
        {
            Assembly = typeof(EmbeddedResourceProcessDefinitionProvider).Assembly,
            BaseNamespace = "Cross.Identity.ProcessEngine.Definitions",
        });
        var step = new CommunicationEndpointAddStep
        {
            Kind = "communicationEndpointAdd",
            UserAccountIdKey = "UserAccountId",
            EmailKey = "Email",
            PhoneKey = "PhoneNumber",
            TtlKey = "Ttl",
            Template = "verify",
            Subject = "Verification Code",
            CommunicationEndpoints = _endpoints.Object,
            CodeService = _codeService.Object,
            NotificationComposer = new NotificationComposer(
                new EmbeddedResourceProcessDefinitionProvider(embeddedOptions),
                Microsoft.Extensions.Options.Options.Create(new NotificationOptions())),
            Configuration = configuration,
            Logger = Mock.Of<ILogger>(),
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointAdd.UserAccountId", userAccountId)
            .Set("communicationEndpointAdd.Email", "second@example.com")
            .Set("communicationEndpointAdd.Ttl", TimeSpan.FromMinutes(5));

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<CommunicationEndpointDto>("communicationEndpointAdd.Endpoint").Should().BeEquivalentTo(endpoint);
        bag.Get<string>("communicationEndpointAdd.LastCode").Should().NotBeNullOrEmpty();
        _codeService.Verify(
            s => s.SendAsync(
                It.Is<NotificationMessage>(m => m.Channel == ChannelEnum.Email && m.Destination == "second@example.com"),
                It.IsAny<string>(),
                userAccountId,
                TimeSpan.FromMinutes(5),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenEmailAndCode_WhenSetVerified_ThenVerifiesAndCompletesAsync()
    {
        var userAccountId = Guid.NewGuid();
        var endpoint = new CommunicationEndpointDto
        {
            Id = Guid.NewGuid(),
            Channel = ChannelEnum.Email,
            Address = "second@example.com",
            IsVerified = true,
            Source = CommunicationEndpointSource.Manual,
        };
        _codeService
            .Setup(s => s.VerifyAsync(
                userAccountId, ChannelEnum.Email, "second@example.com", "ABCDEFGH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _endpoints
            .Setup(s => s.SetVerifiedAsync(
                userAccountId,
                ChannelEnum.Email,
                "second@example.com",
                new HostSuppliedClientContext("1.1.1.1", "ua", null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(endpoint);

        var step = new CommunicationEndpointSetVerifiedStep
        {
            Kind = "communicationEndpointSetVerified",
            UserAccountIdKey = "UserAccountId",
            EmailKey = "Email",
            PhoneKey = "PhoneNumber",
            CodeKey = "Code",
            CommunicationEndpoints = _endpoints.Object,
            CodeService = _codeService.Object,
            Logger = Mock.Of<ILogger>(),
            Next = "done",
        };

        var bag = new Bag()
            .Set("communicationEndpointSetVerified.UserAccountId", userAccountId)
            .Set("communicationEndpointSetVerified.Email", "second@example.com")
            .Set("communicationEndpointSetVerified.Code", "ABCDEFGH")
            .Set("collectForm.IpAddress", "1.1.1.1")
            .Set("collectForm.UserAgent", "ua");

        var result = await step.ExecuteAsync(bag, CancellationToken.None);

        result.Status.Should().Be(StepStatusEnum.Ok);
        bag.Get<bool>("communicationEndpointSetVerified.IsVerified").Should().BeTrue();
        _endpoints.Verify(
            s => s.SetVerifiedAsync(
                userAccountId,
                ChannelEnum.Email,
                "second@example.com",
                new HostSuppliedClientContext("1.1.1.1", "ua", null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
