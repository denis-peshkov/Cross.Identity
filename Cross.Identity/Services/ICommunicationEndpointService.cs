namespace Cross.Identity.Services;

/// <summary>
/// Manages per-user communication endpoints and the single preferred delivery channel.
/// Preferred / trusted delivery uses only <c>IsVerified</c> endpoints; OTP may also fall back
/// to an unverified <c>UsersAccounts.Email</c> or <c>PhoneNumber</c> so a newly added contact can be verified.
/// Used by process steps:
/// <list type="bullet">
/// <item><description><c>SendCodeStep</c> / <c>VerifyCodeStep</c> — <see cref="ResolveOtpTargetAsync"/></description></item>
/// <item><description><c>ResetPasswordStep</c> (notify) — <see cref="ResolveDeliveryTargetAsync"/></description></item>
/// <item><description><c>CommunicationEndpointsGetAllStep</c> — <see cref="GetAllAsync"/></description></item>
/// <item><description><c>CommunicationEndpointSetPreferredStep</c> — <see cref="SetPreferredAsync"/></description></item>
/// </list>
/// </summary>
public interface ICommunicationEndpointService
{
    /// <summary>
    /// List all communication endpoints for a user.
    /// Requires an active refresh token belonging to <paramref name="userAccountId"/> (session proof).
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="refreshToken">Active refresh token for <paramref name="userAccountId"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Endpoints for the user (may be empty).</returns>
    Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userAccountId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a verified endpoint as the only preferred communication target for the user.
    /// Requires an active refresh token belonging to <paramref name="userAccountId"/> (session proof).
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="endpointId">Endpoint id that must belong to <paramref name="userAccountId"/> and be verified.</param>
    /// <param name="refreshToken">Active refresh token for <paramref name="userAccountId"/>.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPreferredAsync(
        Guid userAccountId,
        Guid endpointId,
        string refreshToken,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve where to deliver <em>trusted</em> messages (for example password-changed notify).
    /// Order: <c>Authentication:LockChannelAsEmail</c> → preferred verified endpoint → email
    /// (verified email endpoint, else verified <c>UsersAccounts.Email</c>) → phone
    /// (verified SMS endpoint, else verified <c>UsersAccounts.PhoneNumber</c>).
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Channel and address for delivery.</returns>
    /// <exception cref="ValidationException">
    /// No preferred verified channel and no verified account email or phone (or no email when lock-as-email is on).
    /// </exception>
    Task<DeliveryTarget> ResolveDeliveryTargetAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve OTP send/verify target (same order as <see cref="ResolveDeliveryTargetAsync"/>),
    /// but account email/phone fallback also allows <em>unverified</em> account contacts
    /// (chicken-and-egg for confirmation). Messenger channels map to <see cref="ChannelEnum.Sms"/>
    /// until messenger OTP senders are implemented.
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OTP channel (<see cref="ChannelEnum.Email"/> or <see cref="ChannelEnum.Sms"/>) and address.</returns>
    /// <exception cref="ValidationException">
    /// No preferred verified channel and no account email or phone (or no email when lock-as-email is on).
    /// </exception>
    Task<DeliveryTarget> ResolveOtpTargetAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preferred verified endpoint for the user, or <c>null</c> if none is set.
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preferred endpoint DTO, or <c>null</c>.</returns>
    Task<CommunicationEndpointDto?> GetPreferredAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}
