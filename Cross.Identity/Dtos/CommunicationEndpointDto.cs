namespace Cross.Identity.Dtos;

public sealed class CommunicationEndpointDto
{
    public Guid Id { get; init; }

    public ChannelEnum Channel { get; init; }

    public string Address { get; init; } = null!;

    public bool IsVerified { get; init; }

    public CommunicationEndpointSource Source { get; init; }

    public bool IsPreferred { get; init; }
}
