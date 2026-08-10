namespace Cross.Identity.Tests.Extensions;

[TestFixture]
public sealed class ChannelEnumExtensionsTests
{
    [TestCase(ChannelEnum.Sms, true)]
    [TestCase(ChannelEnum.Telegram, true)]
    [TestCase(ChannelEnum.Viber, true)]
    [TestCase(ChannelEnum.WatsApp, true)]
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
    [TestCase(ChannelEnum.WatsApp, ChannelEnum.Sms)]
    public void GivenChannel_WhenToOtpChannel_ThenMapsMessengersToSms(ChannelEnum input, ChannelEnum expected)
    {
        input.ToOtpChannel().Should().Be(expected);
    }

    [TestCase(ChannelEnum.Email, ChannelEnum.Email)]
    [TestCase(ChannelEnum.Sms, ChannelEnum.Sms)]
    [TestCase(ChannelEnum.Telegram, ChannelEnum.Sms)]
    public void GivenChannel_WhenToEmailOrSmsNotification_ThenMapsPhoneFamilyToSms(ChannelEnum input, ChannelEnum expected)
    {
        input.ToEmailOrSmsNotification().Should().Be(expected);
    }
}
