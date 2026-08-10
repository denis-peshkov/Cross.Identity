namespace Cross.Identity.Entities;

/// <summary>
/// A verified or candidate destination for user communication (email, SMS, messengers).
/// Exactly one endpoint per user may be <see cref="IsPreferred"/> — the default delivery target.
/// </summary>
public class UserCommunicationEndpointEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public CommunicationEndpointSource Source { get; set; }
    /// <summary>
    /// Optional Id of the source entity (e.g. <c>UsersExternalLogins</c> Id when
    /// <see cref="Source"/> is <see cref="CommunicationEndpointSource.ExternalProvider"/>).
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>Delivery channel.</summary>
    public ChannelEnum Channel { get; set; }
    /// <summary>Destination address: email (lowercased), E.164 phone, or messenger chat id.</summary>
    public string Address { get; set; } = null!;
    /// <summary>Only verified endpoints may be preferred or used for communication.</summary>
    public bool IsVerified { get; set; }
    /// <summary>Default communication target for the user. At most one per <see cref="UserAccountId"/>.</summary>
    public bool IsPreferred { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
