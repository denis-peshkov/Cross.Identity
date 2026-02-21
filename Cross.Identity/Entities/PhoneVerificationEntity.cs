namespace Cross.Identity.Entities;

public class PhoneVerificationEntity
{
    public long Id { get; set; }
    public Guid UserAccountId { get; set; }
    public virtual UserAccountEntity UserAccount { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;
    /// <summary>SHA-256 -> 32 байта</summary>
    public byte[] CodeHash { get; set; } = null!;
    public byte CodeLength { get; set; }
    public byte Attempts { get; set; }
    public byte MaxAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
