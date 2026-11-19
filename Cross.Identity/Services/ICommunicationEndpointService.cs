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
/// <item><description><c>CommunicationEndpointAddStep</c> — <see cref="AddAsync"/></description></item>
/// <item><description><c>CommunicationEndpointSetVerifiedStep</c> — <see cref="SetVerifiedAsync"/></description></item>
/// </list>
/// <para>
/// <see cref="GetAllAsync"/> / <see cref="SetPreferredAsync"/> / <see cref="AddAsync"/> / <see cref="SetVerifiedAsync"/>
/// trust <c>userAccountId</c> from the host.
/// The host must authorize the caller for that account (for example via access-token middleware);
/// the library does not require a refresh token as session proof on these APIs.
/// </para>
/// </summary>
public interface ICommunicationEndpointService
{
    /// <summary>
    /// List all communication endpoints for a user.
    /// </summary>
    /// <param name="userAccountId">Local user account id (host must authorize the caller for this account).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Endpoints for the user (may be empty).</returns>
    Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a verified endpoint as the only preferred communication target for the user.
    /// </summary>
    /// <param name="userAccountId">Local user account id (host must authorize the caller for this account).</param>
    /// <param name="endpointId">Endpoint id that must belong to <paramref name="userAccountId"/> and be verified.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata (<see cref="HostSuppliedClientContext"/>); use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetPreferredAsync(
        Guid userAccountId,
        Guid endpointId,
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

    /// <summary>
    /// Add a secondary email or phone: upsert an <em>unverified</em>
    /// <see cref="CommunicationEndpointSource.Manual"/> endpoint for the address.
    /// Does <strong>not</strong> send or verify OTP — stock
    /// <c>CommunicationEndpointAdd</c> calls this, then sends OTP to <paramref name="address"/>
    /// (unlike <see cref="ResolveOtpTargetAsync"/>, which uses preferred/account contacts).
    /// </summary>
    /// <param name="userAccountId">Local user account id (host must authorize the caller for this account).</param>
    /// <param name="channel"><see cref="ChannelEnum.Email"/> or <see cref="ChannelEnum.Sms"/> only.</param>
    /// <param name="address">Raw email or phone; normalized by the implementation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upserted unverified endpoint DTO (resend-safe if the row already exists unverified).</returns>
    /// <exception cref="ValidationException">
    /// Channel does not support OTP, address is empty, or the endpoint is already verified on this account.
    /// </exception>
    /// <exception cref="ConflictException">
    /// Another account already owns this address as a verified endpoint or verified account contact.
    /// </exception>
    /// <exception cref="NotAuthorizedException">Account is disabled or missing.</exception>
    Task<CommunicationEndpointDto> AddAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a secondary email or phone as verified
    /// (<see cref="CommunicationEndpointSource.Manual"/> endpoint upsert with <c>IsVerified = true</c>).
    /// Does <strong>not</strong> validate OTP — the caller (stock <c>CommunicationEndpointSetVerified</c>)
    /// must verify the code first, then call this.
    /// When the address matches the account primary email/phone (or the primary is empty),
    /// also sets the corresponding <c>UsersAccounts</c> verification flag. Does not force preferred.
    /// </summary>
    /// <param name="userAccountId">Local user account id (host must authorize the caller for this account).</param>
    /// <param name="channel"><see cref="ChannelEnum.Email"/> or <see cref="ChannelEnum.Sms"/> only.</param>
    /// <param name="address">Raw email or phone; normalized by the implementation.</param>
    /// <param name="hostSuppliedClientContext">Host-supplied request metadata; use <see cref="HostSuppliedClientContext.Empty"/> when unknown.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified endpoint DTO.</returns>
    /// <exception cref="ValidationException">Channel does not support OTP or address is empty.</exception>
    /// <exception cref="ConflictException">
    /// Another account already owns this address as a verified endpoint or verified account contact.
    /// </exception>
    /// <exception cref="NotAuthorizedException">Account is disabled or missing.</exception>
    Task<CommunicationEndpointDto> SetVerifiedAsync(
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        HostSuppliedClientContext hostSuppliedClientContext,
        CancellationToken cancellationToken = default);
}
