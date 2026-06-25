namespace Cross.Identity.Entities;

/// <summary>
/// Represents a user in the identity system
/// </summary>
public class UserAccountEntity
{
    /// <summary>
    /// Gets or sets the primary key for this user.
    /// </summary>
    [PersonalData]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user name for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the normalized user name for this user.
    /// </summary>
    [ProtectedPersonalData]
    public virtual string? NormalizedUserName { get; set; }

    /// <summary>
    /// Gets or sets the normalized email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets a telephone number for the user.
    /// </summary>
    /// <remarks>хранить в E.164</remarks>
    [ProtectedPersonalData]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// $argon2id$... / $pbkdf2-... / $sha256...
    /// </summary>
    public string? PasswordPhc { get; set; }

    /// <summary>
    /// Gets or sets a personal salt for this user.
    /// </summary>
    // public string? PasswordSalt { get; set; }

    /// <summary>
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    // public byte[]? PasswordHash { get; set; }

    public short PasswordPepperVersion { get; set; }

    // public bool PasswordResetRequired { get; set; }
    // public DateTime? PasswordResetRequiredAt { get; set; }
    // public DateTime? PasswordResetExpiresAt { get; set; }
    // public string? ForcePasswordResetReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when any user lockout ends.
    /// </summary>
    /// <remarks>
    /// A value in the past means the user is not locked out.
    /// </remarks>
    public virtual DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if the user could be locked out.
    /// </summary>
    /// <value>True if the user could be locked out, otherwise false.</value>
    public virtual bool LockoutEnabled { get; set; }

    /// <summary>
    /// Gets or sets the number of failed login attempts for the current user.
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// A random value that must change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public Guid? SecurityStamp { get; set; }

    /// <summary>
    /// A random value that must change whenever a user is persisted to the store
    /// </summary>
    public Guid? ConcurrencyStamp { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their telephone address.
    /// </summary>
    /// <value>True if the telephone number has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool PhoneConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if two factor authentication is enabled for this user.
    /// </summary>
    /// <value>True if 2fa is enabled, otherwise false.</value>
    [PersonalData]
    public bool TwoFactorEnabled { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }

    public virtual ICollection<UserExternalLoginEntity> ExternalLogins { get; set; } = new List<UserExternalLoginEntity>();
}
