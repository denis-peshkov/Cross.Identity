namespace Cross.Identity.Entities;

public class EmailVerificationEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    public required Guid UserAccountId { get; set; }
    public virtual required UserAccountEntity UserAccount { get; set; }

    public string Email { get; set; }
    /// <summary>SHA-256 -> 32 bytes</summary>
    public byte[] TokenHash { get; set; } = null!;
    public byte TokenLength { get; set; }
    public byte Attempts { get; set; }
    public byte MaxAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
