namespace Cross.Identity.Entities;

public class PhoneVerificationEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public string PhoneNumber { get; set; } = null!;
    /// <summary>SHA-256 -> 32 bytes</summary>
    public byte[] CodeHash { get; set; } = null!;
    public byte CodeLength { get; set; }
    public byte Attempts { get; set; }
    public byte MaxAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
