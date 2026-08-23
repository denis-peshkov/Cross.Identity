namespace Cross.Identity.Services;

/// <summary>
/// One-time code (OTP) service: send and verify.
/// Used by steps:
/// <list type="bullet">
/// <item><description><c>SendCodeStep</c> — <see cref="SendAsync"/></description></item>
/// <item><description><c>VerifyCodeStep</c> — <see cref="VerifyAsync"/></description></item>
/// </list>
/// </summary>
internal interface ICodeService
{
    /// <summary>
    /// Send a one-time code to the specified channel/destination with a TTL.
    /// Enforces <c>Authentication:OtpSendRateLimit</c> (cooldown / per-window cap) for the same user + destination.
    /// </summary>
    /// <param name="msg">Notification payload (includes <see cref="NotificationMessage.Channel"/>).</param>
    /// <param name="code">Code text (generated outside the service or by the service — architecture choice).</param>
    /// <param name="userAccountId">User account id.</param>
    /// <param name="ttl">Code lifetime.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="NotSupportedException">Messenger channels (Telegram, Viber, WhatsApp) are not implemented for OTP send.</exception>
    /// <exception cref="ValidationException">OTP send rate limit exceeded (cooldown or window cap).</exception>
    Task SendAsync(
        NotificationMessage msg,
        string code,
        Guid userAccountId,
        TimeSpan ttl,
        CancellationToken cancellationToken);

    /// <summary>
    /// Verify a one-time code for the specified user, channel and identity.
    /// The verification row must belong to <paramref name="userAccountId"/>.
    /// </summary>
    /// <param name="userAccountId">User account that owns the OTP.</param>
    /// <param name="channel">Delivery channel (<see cref="ChannelEnum.Email"/> / <see cref="ChannelEnum.Sms"/>, …).</param>
    /// <param name="identity">Identity (email address, phone, etc.).</param>
    /// <param name="code">Presented code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the code is valid and not expired; otherwise <c>false</c>.</returns>
    Task<bool> VerifyAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string identity,
        string code,
        CancellationToken cancellationToken);
}
