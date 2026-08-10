namespace Cross.Identity.Entities;

public class ExternalLoginStateEntity : IHasConcurrencyStamp
{
    public Guid Id { get; set; }

    /// <summary>Set when linking an external login to an existing account.</summary>
    public Guid? UserAccountId { get; set; }
    public virtual UserAccountEntity? UserAccount { get; set; }

    public string Nonce { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string? ReturnUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }
}
