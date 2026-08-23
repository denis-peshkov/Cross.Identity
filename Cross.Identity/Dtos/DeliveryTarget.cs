namespace Cross.Identity.Dtos;

/// <summary>
/// Resolved delivery channel and address for OTP / notifications.
/// </summary>
public sealed class DeliveryTarget
{
    public required ChannelEnum Channel { get; init; }

    public required string Address { get; init; }
}
