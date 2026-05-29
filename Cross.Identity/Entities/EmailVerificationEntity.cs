namespace Cross.Identity.Entities;

public class EmailVerificationEntity
{
    public long Id { get; set; }
    public Guid UserAccountId { get; set; }
    public virtual UserAccountEntity UserAccount { get; set; }
    public string NormalizedEmail { get; set; }
    /// <summary>SHA-256 -> 32 байта</summary>
    public byte[] TokenHash { get; set; } = null!;
    public byte TokenLength { get; set; }
    public byte Attempts { get; set; }
    public byte MaxAttempts { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
