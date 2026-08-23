namespace Cross.Identity.Extensions;

/// <summary>
/// Helpers for <see cref="ChannelEnum"/> — OTP generation, address normalization, and channel grouping.
/// </summary>
public static class ChannelEnumExtensions
{
    /// <summary>Channels treated as phone/messenger delivery (mapped to SMS for OTP until dedicated senders exist).</summary>
    public static readonly ChannelEnum[] PhoneChannels =
    {
        ChannelEnum.Sms,
        ChannelEnum.Telegram,
        ChannelEnum.Viber,
        ChannelEnum.WhatsApp,
    };

    /// <summary>Returns <c>true</c> when <paramref name="channel"/> is a phone or messenger channel.</summary>
    public static bool IsPhoneChannel(this ChannelEnum channel) =>
        PhoneChannels.Contains(channel);

    /// <summary>Returns <c>true</c> when the channel supports one-time codes (email or SMS).</summary>
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

    /// <summary>
    /// Trims the address and lowercases email; leaves phone/messenger values as trimmed text.
    /// </summary>
    /// <param name="channel">Delivery channel (determines normalization rules).</param>
    /// <param name="address">Raw address from the caller.</param>
    /// <returns>Normalized address suitable for storage or lookup.</returns>
    public static string NormalizeAddress(this ChannelEnum channel, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        var trimmed = address.Trim();
        return channel == ChannelEnum.Email ? trimmed.ToLowerInvariant() : trimmed;
    }
}
