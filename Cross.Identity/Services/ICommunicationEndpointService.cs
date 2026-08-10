namespace Cross.Identity.Services;

/// <summary>
/// Manages per-user communication endpoints and the single preferred delivery channel.
/// Communication is allowed only to <c>IsVerified</c> endpoints.
/// </summary>
public interface ICommunicationEndpointService
{
    /// <summary>List all endpoints for a user.</summary>
    Task<IReadOnlyList<CommunicationEndpointDto>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);

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
    /// <param name="ipAddress">Optional client IP for audit.</param>
    /// <param name="userAgent">Optional User-Agent for audit.</param>
    /// <param name="deviceFingerprint">Optional device fingerprint for audit.</param>
    Task SetPreferredAsync(
        Guid userId,
        Guid endpointId,
        string? ipAddress = null,
        string? userAgent = null,
        string? deviceFingerprint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolve delivery channel for an identity selector (login field + value).
    /// Email → <see cref="ChannelEnum.Email"/>; phone → preferred phone-channel endpoint for that address, else SMS;
    /// user name → preferred verified endpoint channel (required).
    /// </summary>
    Task<ChannelEnum> ResolveDeliveryChannelAsync(
        Guid userId,
        string selectorField,
        string selectorValue,
        ChannelEnum? fallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Like <see cref="ResolveDeliveryChannelAsync"/>, but maps messenger channels to
    /// <see cref="ChannelEnum.Sms"/> for OTP until messenger senders are implemented.
    /// </summary>
    Task<ChannelEnum> ResolveOtpChannelAsync(
        Guid userId,
        string selectorField,
        string selectorValue,
        ChannelEnum? fallback = null,
        CancellationToken cancellationToken = default);

    /// <summary>Preferred verified endpoint, or null if none.</summary>
    Task<CommunicationEndpointDto?> GetPreferredAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync account email/phone into endpoints after confirmation flags change.
    /// </summary>
    Task SyncAccountContactsAsync(Guid userId, CancellationToken cancellationToken = default);
}
