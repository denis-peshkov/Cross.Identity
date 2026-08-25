namespace Cross.Identity.Dtos;

/// <summary>
/// Resolved delivery channel and address for OTP / notifications.
/// Produced by <see cref="Services.ICommunicationEndpointService.ResolveDeliveryTargetAsync"/>
/// and <see cref="Services.ICommunicationEndpointService.ResolveOtpTargetAsync"/>.
/// </summary>
public sealed class DeliveryTarget
{
    /// <summary>Channel to use for send (<see cref="ChannelEnum.Email"/> or <see cref="ChannelEnum.Sms"/> for OTP).</summary>
    public required ChannelEnum Channel { get; init; }

    /// <summary>Destination address for that channel.</summary>
    public required string Address { get; init; }
}
