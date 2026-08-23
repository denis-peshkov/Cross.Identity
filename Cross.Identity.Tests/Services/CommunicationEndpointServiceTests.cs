namespace Cross.Identity.Tests.Services;

[TestFixture]
public class CommunicationEndpointServiceTests : EFTestsBase
{
    private const string SessionRefresh = "session-refresh-token-for-tests";

    private CommunicationEndpointService _service = null!;
    private Mock<IJwtTokenService> _jwt = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _jwt = new Mock<IJwtTokenService>();
        _jwt
            .Setup(j => j.EnsureRefreshTokenBelongsToUserAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _service = new CommunicationEndpointService(Context, new AuditService(Context), _jwt.Object, TestAuthOptions.Snapshot());
    }

    [Test]
    public async Task Upsert_FirstVerified_BecomesPreferred()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "a@example.com", EmailVerified = true });

        var dto = await _service.UpsertAsync(
            userId, ChannelEnum.Email, "a@example.com", CommunicationEndpointSource.Account, isVerified: true);

        dto.IsPreferred.Should().BeTrue();
        dto.IsVerified.Should().BeTrue();
        dto.Address.Should().Be("a@example.com");
    }

    [Test]
    public async Task SetPreferred_OnlyVerified_AndClearsPrevious()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "a@example.com", PhoneNumber = "+79161234567" });

        var email = await _service.UpsertAsync(
            userId, ChannelEnum.Email, "a@example.com", CommunicationEndpointSource.Account, true);
        var sms = await _service.UpsertAsync(
            userId, ChannelEnum.Sms, "+79161234567", CommunicationEndpointSource.Account, true);
        var telegram = await _service.UpsertAsync(
            userId, ChannelEnum.Telegram, "+79161234567", CommunicationEndpointSource.LinkedMessenger, true);

        email.IsPreferred.Should().BeTrue();

        await _service.SetPreferredAsync(userId, telegram.Id, SessionRefresh, new ClientContext("10.0.0.1", "ua", "fp"));

        var all = await _service.GetAllAsync(userId, SessionRefresh);
        all.Single(x => x.Id == telegram.Id).IsPreferred.Should().BeTrue();
        all.Where(x => x.Id != telegram.Id).Should().OnlyContain(x => !x.IsPreferred);

        Context.Audits.Should().Contain(a =>
            a.Operation == AuditOperation.CommunicationEndpointChanged
            && a.EntityId == telegram.Id.ToString()
            && a.IpAddress == "10.0.0.1"
            && a.UserAgent == "ua"
            && a.DeviceFingerprint == "fp");
    }

    [Test]
    public async Task SetPreferred_WhenNotVerified_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId });
        var ep = await _service.UpsertAsync(
            userId, ChannelEnum.Email, "x@example.com", CommunicationEndpointSource.Manual, isVerified: false);

        var act = () => _service.SetPreferredAsync(userId, ep.Id, SessionRefresh, ClientContext.Empty);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*verified*");
    }

    [Test]
    public async Task ResolveDeliveryTarget_UsesPreferredMessenger()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone, PhoneNumberVerified = true });

        await _service.UpsertAsync(userId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, true);
        var tg = await _service.UpsertAsync(
            userId, ChannelEnum.Telegram, phone, CommunicationEndpointSource.LinkedMessenger, true);
        await _service.SetPreferredAsync(userId, tg.Id, SessionRefresh, ClientContext.Empty);

        var target = await _service.ResolveDeliveryTargetAsync(userId);

        target.Channel.Should().Be(ChannelEnum.Telegram);
        target.Address.Should().Be(phone);
    }

    [Test]
    public async Task ResolveOtpTarget_Messenger_FallsBackToSms()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });

        var tg = await _service.UpsertAsync(
            userId, ChannelEnum.Telegram, phone, CommunicationEndpointSource.LinkedMessenger, true);
        await _service.SetPreferredAsync(userId, tg.Id, SessionRefresh, ClientContext.Empty);

        var otp = await _service.ResolveOtpTargetAsync(userId);

        otp.Channel.Should().Be(ChannelEnum.Sms);
        otp.Address.Should().Be(phone);
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenNoPreferred_FallsBackToVerifiedEmail()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "fallback@example.com",
            EmailVerified = true,
        });

        // Verified email endpoint that is not preferred (clear preferred after upsert of phone-only preferred then remove)
        await _service.UpsertAsync(
            userId, ChannelEnum.Email, "fallback@example.com", CommunicationEndpointSource.Account, true);
        var phone = await _service.UpsertAsync(
            userId, ChannelEnum.Sms, "+79161234567", CommunicationEndpointSource.Account, true);
        await _service.SetPreferredAsync(userId, phone.Id, SessionRefresh, ClientContext.Empty);

        // Clear preferred flags to simulate missing preferred
        foreach (var row in Context.UsersCommunicationEndpoints.Where(x => x.UserAccountId == userId))
        {
            row.IsPreferred = false;
        }

        await Context.SaveChangesAsync();

        var target = await _service.ResolveDeliveryTargetAsync(userId);

        target.Channel.Should().Be(ChannelEnum.Email);
        target.Address.Should().Be("fallback@example.com");
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenLockChannelAsEmail_ForcesEmail()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "locked@example.com",
            EmailVerified = true,
            PhoneNumber = phone,
            PhoneNumberVerified = true,
        });

        var locked = new CommunicationEndpointService(
            Context,
            new AuditService(Context),
            _jwt.Object,
            Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions { LockChannelAsEmail = true }));

        await locked.UpsertAsync(userId, ChannelEnum.Email, "locked@example.com", CommunicationEndpointSource.Account, true);
        var sms = await locked.UpsertAsync(userId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, true);
        await locked.SetPreferredAsync(userId, sms.Id, SessionRefresh, ClientContext.Empty);

        var target = await locked.ResolveDeliveryTargetAsync(userId);

        target.Channel.Should().Be(ChannelEnum.Email);
        target.Address.Should().Be("locked@example.com");
    }

    [Test]
    public async Task ResolveOtpTarget_WhenAccountEmailUnverified_AllowsFallback()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "new@example.com",
            EmailVerified = false,
        });

        var otp = await _service.ResolveOtpTargetAsync(userId);

        otp.Channel.Should().Be(ChannelEnum.Email);
        otp.Address.Should().Be("new@example.com");
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenAccountEmailUnverified_DoesNotFallback()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "new@example.com",
            EmailVerified = false,
        });

        var act = () => _service.ResolveDeliveryTargetAsync(userId);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*verified email or phone*");
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenPhoneOnlyVerified_FallsBackToAccountPhone()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            PhoneNumber = phone,
            PhoneNumberVerified = true,
        });

        var target = await _service.ResolveDeliveryTargetAsync(userId);

        target.Channel.Should().Be(ChannelEnum.Sms);
        target.Address.Should().Be(phone);
    }

    [Test]
    public async Task ResolveOtpTarget_WhenPhoneOnlyUnverified_AllowsFallback()
    {
        var userId = Guid.NewGuid();
        var phone = "+79169876543";
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            PhoneNumber = phone,
            PhoneNumberVerified = false,
        });

        var otp = await _service.ResolveOtpTargetAsync(userId);

        otp.Channel.Should().Be(ChannelEnum.Sms);
        otp.Address.Should().Be(phone);
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenPhoneOnlyUnverified_DoesNotFallback()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            PhoneNumber = "+79161112233",
            PhoneNumberVerified = false,
        });

        var act = () => _service.ResolveDeliveryTargetAsync(userId);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*verified email or phone*");
    }

    [Test]
    public async Task ResolveDeliveryTarget_WhenLockChannelAsEmailAndUnverified_Throws()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "new@example.com",
            EmailVerified = false,
        });

        var locked = new CommunicationEndpointService(
            Context,
            new AuditService(Context),
            _jwt.Object,
            Microsoft.Extensions.Options.Options.Create(new AuthenticationOptions { LockChannelAsEmail = true }));

        var act = () => locked.ResolveDeliveryTargetAsync(userId);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*verified email*");
    }

    [Test]
    public async Task SyncAccountContacts_CreatesVerifiedEndpoints()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "sync@example.com",
            EmailVerified = true,
            PhoneNumber = "+40722123456",
            PhoneNumberVerified = true,
        });

        await _service.SyncAccountContactsAsync(userId);

        var all = await _service.GetAllAsync(userId, SessionRefresh);
        all.Should().HaveCount(2);
        all.Should().Contain(x => x.Channel == ChannelEnum.Email && x.IsVerified);
        all.Should().Contain(x => x.Channel == ChannelEnum.Sms && x.IsVerified);
        all.Count(x => x.IsPreferred).Should().Be(1);
    }
}
