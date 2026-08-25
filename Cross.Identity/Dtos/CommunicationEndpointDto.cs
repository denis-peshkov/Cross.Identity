namespace Cross.Identity.Dtos;

/// <summary>
/// One communication endpoint for a user (email, SMS, or messenger).
/// Returned by <see cref="Services.ICommunicationEndpointService.GetAllAsync"/> /
/// <see cref="Services.ICommunicationEndpointService.GetPreferredAsync"/> and by stock
/// <c>CommunicationEndpointsGetAll</c> flows.
/// </summary>
public sealed class CommunicationEndpointDto
{
    /// <summary>Endpoint id (<c>auth.UsersCommunicationEndpoints</c> PK).</summary>
    public Guid Id { get; init; }

    /// <summary>Delivery channel for this address.</summary>
    public ChannelEnum Channel { get; init; }

    /// <summary>
    /// Channel-specific address (verified email, E.164 phone, or messenger chat id).
    /// </summary>
    public string Address { get; init; } = null!;

    /// <summary>
    /// Whether the address has been verified (OTP / OAuth attestation / host).
    /// Preferred delivery uses only verified endpoints.
    /// </summary>
    public bool IsVerified { get; init; }

    /// <summary>How the address was introduced (account, OAuth, messenger, manual).</summary>
    public CommunicationEndpointSource Source { get; init; }

    /// <summary>
    /// <c>true</c> when this is the single preferred delivery target for the user.
    /// </summary>
    public bool IsPreferred { get; init; }
}
