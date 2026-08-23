namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public sealed class ChannelEnumExtensionsTests
{
    [TestCase(ChannelEnum.Sms, true)]
    [TestCase(ChannelEnum.Telegram, true)]
    [TestCase(ChannelEnum.Viber, true)]
    [TestCase(ChannelEnum.WhatsApp, true)]
    [TestCase(ChannelEnum.Email, false)]
    public void GivenChannel_WhenIsPhoneChannel_ThenReturnsExpected(ChannelEnum channel, bool expected)
    {
        channel.IsPhoneChannel().Should().Be(expected);
    }

    [TestCase(ChannelEnum.Email, true)]
    [TestCase(ChannelEnum.Sms, true)]
    [TestCase(ChannelEnum.Telegram, false)]
    public void GivenChannel_WhenSupportsOtp_ThenReturnsExpected(ChannelEnum channel, bool expected)
    {
        channel.SupportsOtp().Should().Be(expected);
    }

    [TestCase(ChannelEnum.Email, ChannelEnum.Email)]
    [TestCase(ChannelEnum.Sms, ChannelEnum.Sms)]
    [TestCase(ChannelEnum.Telegram, ChannelEnum.Sms)]
    [TestCase(ChannelEnum.Viber, ChannelEnum.Sms)]
    [TestCase(ChannelEnum.WhatsApp, ChannelEnum.Sms)]
    public void GivenChannel_WhenToEmailOrSms_ThenMapsPhoneFamilyToSms(ChannelEnum input, ChannelEnum expected)
    {
        input.ToEmailOrSms().Should().Be(expected);
    }

    [Test]
    public void GivenEmail_WhenGenerateCode_ThenReturnsAlphanumeric()
    {
        var code = ChannelEnum.Email.GenerateCode();
        code.Should().HaveLength(8);
        code.Should().MatchRegex("^[A-Z0-9]+$");
    }

    [Test]
    public void GivenSms_WhenGenerateCode_ThenReturnsDigits()
    {
        var code = ChannelEnum.Sms.GenerateCode();
        code.Should().HaveLength(6);
        code.Should().MatchRegex("^[0-9]+$");
    }

    [Test]
    public void GivenEmail_WhenNormalizeAddress_ThenLowercases()
    {
        ChannelEnum.Email.NormalizeAddress("  User@Example.COM ").Should().Be("user@example.com");
    }

    [Test]
    public void GivenSms_WhenNormalizeAddress_ThenTrimsOnly()
    {
        ChannelEnum.Sms.NormalizeAddress("  +79161234567 ").Should().Be("+79161234567");
    }
}
