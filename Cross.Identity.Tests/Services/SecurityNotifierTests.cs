namespace Cross.Identity.Tests.Services;

[TestFixture]
public class SecurityNotifierTests
{
    [Test]
    [Category(TestCategory.UNIT)]
    public async Task SendAsync_WhenEmail_CallsEmailSender()
    {
        var email = new Mock<IEmailSenderService>();
        var sms = new Mock<ISmsSenderService>();
        var sut = new SecurityNotifier(email.Object, sms.Object);

        await sut.SendAsync(ChannelEnum.Email, "a@b.c", "Subj", "text", "<p>html</p>");

        email.Verify(
            e => e.SendAsync("", "a@b.c", "Subj", "text", "<p>html</p>", It.IsAny<CancellationToken>()),
            Times.Once);
        sms.Verify(
            s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task SendAsync_WhenSms_CallsSmsSender()
    {
        var email = new Mock<IEmailSenderService>();
        var sms = new Mock<ISmsSenderService>();
        var sut = new SecurityNotifier(email.Object, sms.Object);

        await sut.SendAsync(ChannelEnum.Sms, "+10000000000", "Subj", "text", "ignored");

        sms.Verify(
            s => s.SendAsync("+10000000000", "text", It.IsAny<CancellationToken>()),
            Times.Once);
        email.Verify(
            e => e.SendAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
