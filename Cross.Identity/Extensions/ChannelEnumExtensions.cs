namespace Cross.Identity.Extensions;

public static class ChannelEnumExtensions
{
    public static readonly ChannelEnum[] PhoneChannels =
    {
        ChannelEnum.Sms,
        ChannelEnum.Telegram,
        ChannelEnum.Viber,
        ChannelEnum.WhatsApp,
    };

    public static bool IsPhoneChannel(this ChannelEnum channel) =>
        PhoneChannels.Contains(channel);

    public static bool SupportsOtp(this ChannelEnum channel) =>
        channel is ChannelEnum.Email or ChannelEnum.Sms;

    /// <summary>
    /// Maps messenger channels to <see cref="ChannelEnum.Sms"/>; leaves Email/Sms (and others) as-is.
    /// Used for OTP persistence and notifications until messenger senders exist.
    /// </summary>
    public static ChannelEnum ToEmailOrSms(this ChannelEnum channel) =>
        channel.IsPhoneChannel() ? ChannelEnum.Sms : channel;

    /// <summary>OTP code: numeric for SMS, alphanumeric for email.</summary>
    public static string GenerateCode(this ChannelEnum channel) =>
        channel == ChannelEnum.Sms
            ? CodeGeneratorHelper.GenerateNumericCode()
            : CodeGeneratorHelper.GenerateCode();

    public static string NormalizeAddress(this ChannelEnum channel, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        var trimmed = address.Trim();
        return channel == ChannelEnum.Email ? trimmed.ToLowerInvariant() : trimmed;
    }
}
