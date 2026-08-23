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
    /// Requires an active refresh token belonging to <paramref name="userId"/> (session proof).
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="refreshToken">Active refresh token for <paramref name="userId"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Endpoints for the user (may be empty).</returns>
    Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert or update an endpoint for the user.
    /// When <paramref name="isVerified"/> is <c>true</c> and the user has no preferred endpoint yet,
    /// the new/updated endpoint becomes preferred.
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="channel">Delivery channel (email, SMS, messenger, …).</param>
    /// <param name="address">Channel-specific address (normalized by the implementation).</param>
    /// <param name="source">How the endpoint was obtained (account sync, manual, external provider, …).</param>
    /// <param name="isVerified">Whether the address has been attested for this user.</param>
    /// <param name="entityId">
    /// Optional related entity id (for example external-login id when syncing a provider email).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upserted endpoint DTO.</returns>
    Task<CommunicationEndpointDto> UpsertAsync(
        Guid userId,
        ChannelEnum channel,
        string address,
        CommunicationEndpointSource source,
        bool isVerified,
        Guid? entityId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a verified endpoint as the only preferred communication target for the user.
    /// Requires an active refresh token belonging to <paramref name="userId"/> (session proof).
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="endpointId">Endpoint id that must belong to <paramref name="userId"/> and be verified.</param>
    /// <param name="refreshToken">Active refresh token for <paramref name="userId"/>.</param>
    /// <param name="clientContext">Client metadata recorded in audit (use <see cref="ClientContext.Empty"/> when unknown).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPreferredAsync(
        Guid userId,
        Guid endpointId,
        string refreshToken,
        ClientContext clientContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve where to deliver <em>trusted</em> messages (for example password-changed notify).
    /// Order: <c>Authentication:LockChannelAsEmail</c> → preferred verified endpoint → email
    /// (verified email endpoint, else verified <c>UsersAccounts.Email</c>) → phone
    /// (verified SMS endpoint, else verified <c>UsersAccounts.PhoneNumber</c>).
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Channel and address for delivery.</returns>
    /// <exception cref="ValidationException">
    /// No preferred verified channel and no verified account email or phone (or no email when lock-as-email is on).
    /// </exception>
    Task<DeliveryTarget> ResolveDeliveryTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve OTP send/verify target (same order as <see cref="ResolveDeliveryTargetAsync"/>),
    /// but account email/phone fallback also allows <em>unverified</em> account contacts
    /// (chicken-and-egg for confirmation). Messenger channels map to <see cref="ChannelEnum.Sms"/>
    /// until messenger OTP senders are implemented.
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>OTP channel (<see cref="ChannelEnum.Email"/> or <see cref="ChannelEnum.Sms"/>) and address.</returns>
    /// <exception cref="ValidationException">
    /// No preferred verified channel and no account email or phone (or no email when lock-as-email is on).
    /// </exception>
    Task<DeliveryTarget> ResolveOtpTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Preferred verified endpoint for the user, or <c>null</c> if none is set.
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Preferred endpoint DTO, or <c>null</c>.</returns>
    Task<CommunicationEndpointDto?> GetPreferredAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert verified endpoints from <c>UsersAccounts</c> email/phone when the corresponding
    /// verification flags are set (<c>EmailVerified</c> / <c>PhoneNumberVerified</c>).
    /// Called after successful code validation that verifies those contacts.
    /// </summary>
    /// <param name="userId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SyncAccountContactsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
