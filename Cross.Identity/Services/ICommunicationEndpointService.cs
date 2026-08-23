namespace Cross.Identity.Services;

/// <summary>
/// Manages per-user communication endpoints and the single preferred delivery channel.
/// Communication is allowed only to <c>IsVerified</c> endpoints.
/// </summary>
public interface ICommunicationEndpointService
{
    /// <summary>List all endpoints for a user.</summary>
    Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert an endpoint. When <paramref name="isVerified"/> is true and the user has no preferred
    /// endpoint yet, the new/updated endpoint becomes preferred.
    /// </summary>
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
    /// </summary>
    /// <param name="clientContext">Optional client metadata for audit.</param>
    Task SetPreferredAsync(
        Guid userId,
        Guid endpointId,
        string refreshToken,
        ClientContext clientContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve where to deliver messages for the user.
    /// Order: <c>Authentication:LockChannelAsEmail</c> → preferred verified endpoint → email
    /// (verified email endpoint, else <c>UsersAccounts.Email</c> if present).
    /// </summary>
    Task<DeliveryTarget> ResolveDeliveryTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Like <see cref="ResolveDeliveryTargetAsync"/>, but maps messenger channels to
    /// <see cref="ChannelEnum.Sms"/> for OTP until messenger senders are implemented.
    /// </summary>
    Task<DeliveryTarget> ResolveOtpTargetAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Preferred verified endpoint, or null if none.</summary>
    Task<CommunicationEndpointDto?> GetPreferredAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync account email/phone into endpoints after confirmation flags change.
    /// </summary>
    Task SyncAccountContactsAsync(Guid userId, CancellationToken cancellationToken = default);
}
