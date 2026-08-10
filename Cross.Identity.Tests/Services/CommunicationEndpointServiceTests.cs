namespace Cross.Identity.Tests.Services;

[TestFixture]
public class CommunicationEndpointServiceTests : EFTestsBase
{
    private CommunicationEndpointService _service = null!;

    [SetUp]
    public override void Setup()
    {
        base.Setup();
        _service = new CommunicationEndpointService(Context, new AuditService(Context));
    }

    [Test]
    public async Task Upsert_FirstVerified_BecomesPreferred()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity { Id = userId, Email = "a@example.com", EmailConfirmed = true });

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

        await _service.SetPreferredAsync(userId, telegram.Id, new ClientContext("10.0.0.1", "ua", "fp"));

        var all = await _service.GetAllAsync(userId);
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

        var act = () => _service.SetPreferredAsync(userId, ep.Id, ClientContext.Empty);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*verified*");
    }

    [Test]
    public async Task ResolveDeliveryChannel_Phone_UsesPreferredMessenger()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone, PhoneNumberConfirmed = true });

        await _service.UpsertAsync(userId, ChannelEnum.Sms, phone, CommunicationEndpointSource.Account, true);
        var tg = await _service.UpsertAsync(
            userId, ChannelEnum.Telegram, phone, CommunicationEndpointSource.LinkedMessenger, true);
        await _service.SetPreferredAsync(userId, tg.Id, ClientContext.Empty);

        var channel = await _service.ResolveDeliveryChannelAsync(userId, "PhoneNumber", phone);

        channel.Should().Be(ChannelEnum.Telegram);
    }

    [Test]
    public async Task ResolveOtpChannel_Messenger_FallsBackToSms()
    {
        var userId = Guid.NewGuid();
        var phone = "+79161234567";
        AddToDb(new UserAccountEntity { Id = userId, PhoneNumber = phone });

        var tg = await _service.UpsertAsync(
            userId, ChannelEnum.Telegram, phone, CommunicationEndpointSource.LinkedMessenger, true);
        await _service.SetPreferredAsync(userId, tg.Id, ClientContext.Empty);

        var otp = await _service.ResolveOtpChannelAsync(userId, "PhoneNumber", phone);

        otp.Should().Be(ChannelEnum.Sms);
    }

    [Test]
    public async Task SyncAccountContacts_CreatesVerifiedEndpoints()
    {
        var userId = Guid.NewGuid();
        AddToDb(new UserAccountEntity
        {
            Id = userId,
            Email = "sync@example.com",
            EmailConfirmed = true,
            PhoneNumber = "+40722123456",
            PhoneNumberConfirmed = true,
        });

        await _service.SyncAccountContactsAsync(userId);

        var all = await _service.GetAllAsync(userId);
        all.Should().HaveCount(2);
        all.Should().Contain(x => x.Channel == ChannelEnum.Email && x.IsVerified);
        all.Should().Contain(x => x.Channel == ChannelEnum.Sms && x.IsVerified);
        all.Count(x => x.IsPreferred).Should().Be(1);
    }


}
