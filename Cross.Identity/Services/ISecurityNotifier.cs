namespace Cross.Identity.Services;

/// <summary>
/// Sends a composed security / FYI notification (no OTP persistence).
/// Used after mutations such as password change; OTP still goes through <see cref="ICodeService"/>.
/// </summary>
internal interface ISecurityNotifier
{
    /// <summary>
    /// Delivers <paramref name="textBody"/> / <paramref name="htmlBody"/> to
    /// <paramref name="address"/> on <paramref name="channel"/> (Email or Sms).
    /// </summary>
    Task SendAsync(
        ChannelEnum channel,
        string address,
        string subject,
        string textBody,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
