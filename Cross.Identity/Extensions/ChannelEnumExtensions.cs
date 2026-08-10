namespace Cross.Identity.Extensions;

public static class ChannelEnumExtensions
{
    public static readonly ChannelEnum[] PhoneChannels =
    {
        ChannelEnum.Sms,
        ChannelEnum.Telegram,
        ChannelEnum.Viber,
        ChannelEnum.WatsApp,
    };

    public static bool IsPhoneChannel(this ChannelEnum channel) =>
        channel is ChannelEnum.Sms or ChannelEnum.Telegram or ChannelEnum.Viber or ChannelEnum.WatsApp;

    public static bool SupportsOtp(this ChannelEnum channel) =>
        channel is ChannelEnum.Email or ChannelEnum.Sms;

    /// <summary>Maps delivery channel to OTP persistence channel (messengers → <see cref="ChannelEnum.Sms"/>).</summary>
    public static ChannelEnum ToOtpChannel(this ChannelEnum channel) =>
        channel switch
        {
            ChannelEnum.Email => ChannelEnum.Email,
            ChannelEnum.Sms => ChannelEnum.Sms,
            _ when channel.IsPhoneChannel() => ChannelEnum.Sms,
            _ => ChannelEnum.Email,
        };

    /// <summary>Maps delivery channel to Email/Sms when messenger senders are unavailable.</summary>
    public static ChannelEnum ToEmailOrSmsNotification(this ChannelEnum channel) =>
        channel.IsPhoneChannel() ? ChannelEnum.Sms : channel;
}
