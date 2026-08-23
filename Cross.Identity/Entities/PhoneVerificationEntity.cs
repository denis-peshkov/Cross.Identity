namespace Cross.Identity.Entities;

/// <summary>
/// Pending SMS/phone OTP verification for a user account.
/// </summary>
public class PhoneVerificationEntity : IHasConcurrencyStamp
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Account the code belongs to.</summary>
    public required Guid UserAccountId { get; set; }

    /// <summary>Account navigation.</summary>
    public virtual required UserAccountEntity UserAccount { get; set; }

    /// <summary>E.164 phone number being verified.</summary>
    public string PhoneNumber { get; set; } = null!;

    /// <summary>SHA-256 hash of the OTP (32 bytes).</summary>
    public byte[] CodeHash { get; set; } = null!;

    /// <summary>Length of the plain code at issue time.</summary>
    public byte CodeLength { get; set; }

    /// <summary>Failed verification attempts so far.</summary>
    public byte Attempts { get; set; }

    /// <summary>Maximum allowed attempts before the row is rejected.</summary>
    public byte MaxAttempts { get; set; }

    /// <summary>UTC expiry after which the code is invalid.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>UTC time the code was successfully consumed, if any.</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>UTC time the row was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
