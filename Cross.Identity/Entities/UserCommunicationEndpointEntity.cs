namespace Cross.Identity.Entities;

/// <summary>
/// A verified or candidate destination for user communication (email, SMS, messengers).
/// Exactly one endpoint per user may be <see cref="IsPreferred"/> — the default delivery target.
/// </summary>
public class UserCommunicationEndpointEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public virtual UserAccountEntity UserAccount { get; set; } = null!;

    /// <summary>Delivery channel.</summary>
    public ChannelEnum Channel { get; set; }

    /// <summary>
    /// Destination address: email (lowercased), E.164 phone, or messenger chat id.
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>Only verified endpoints may be preferred or used for communication.</summary>
    public bool IsVerified { get; set; }

    public CommunicationEndpointSource Source { get; set; }

    /// <summary>
    /// Optional source reference (e.g. <c>UsersExternalLogins.UserExternalLoginId</c>).
    /// </summary>
    public long? SourceRefId { get; set; }

    /// <summary>Default communication target for the user. At most one per <see cref="UserId"/>.</summary>
    public bool IsPreferred { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
