namespace Cross.Identity.Entities;

/// <summary>
/// Model for storing access tokens
/// </summary>
public class AccessTokenEntity : IHasConcurrencyStamp
{
    /// <summary>Jti</summary>
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>When set, the token is no longer valid. Details go to <see cref="AuditEntity"/>.</summary>
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
