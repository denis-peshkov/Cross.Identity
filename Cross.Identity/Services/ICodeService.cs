namespace Cross.Identity.Services;

/// <summary>
/// One-time code (OTP) service: send and verify.
/// Used by steps:
/// <list type="bullet">
/// <item><description><c>SendCodeStep</c> — <see cref="SendAsync"/></description></item>
/// <item><description><c>VerifyCodeStep</c> and <c>CodeAuthStep</c> — <see cref="VerifyAsync"/></description></item>
/// </list>
/// </summary>
internal interface ICodeService
{
    /// <summary>
    /// Send a one-time code to the specified channel/destination with a TTL.
    /// </summary>
    /// <param name="channel">Delivery channel (e.g. <c>"email"</c> or <c>"phone"</c>).</param>
    /// <param name="destination">Destination (e.g. email address or phone number).</param>
    /// <param name="code">Code text (generated outside the service or by the service — architecture choice).</param>
    /// <param name="msg"></param>
    /// <param name="userId"></param>
    /// <param name="ttl">Code lifetime.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendAsync(NotificationMessage msg, string code, string userId, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Verify a one-time code for the specified channel and identity.
    /// </summary>
    /// <param name="channel">Channel (e.g. <c>"email"</c>/<c>"phone"</c>).</param>
    /// <param name="identity">Identity (email address, phone, etc.).</param>
    /// <param name="code">Presented code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the code is valid and not expired; otherwise <c>false</c>.</returns>
    Task<bool> VerifyAsync(string channel, string identity, string code, CancellationToken cancellationToken);
}
