namespace Cross.Identity.Services;

/// <summary>
/// Internal write/sync for communication endpoints. Not exposed on the public
/// <see cref="ICommunicationEndpointService"/> — callers must be pre-authorized
/// library paths (OAuth provider sync, account contact sync after OTP verification).
/// </summary>
internal interface ICommunicationEndpointUpsertService
{
    /// <summary>
    /// Insert or update an endpoint for the user.
    /// When <paramref name="isVerified"/> is <c>true</c> and the user has no preferred endpoint yet,
    /// the new/updated endpoint becomes preferred.
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
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
        Guid userAccountId,
        ChannelEnum channel,
        string address,
        CommunicationEndpointSource source,
        bool isVerified,
        Guid? entityId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert verified endpoints from <c>UsersAccounts</c> email/phone when the corresponding
    /// verification flags are set (<c>EmailVerified</c> / <c>PhoneNumberVerified</c>).
    /// Called after successful code validation that verifies those contacts.
    /// </summary>
    /// <param name="userAccountId">Local user account id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SyncAccountContactsAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}
