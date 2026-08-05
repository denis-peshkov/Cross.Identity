namespace Cross.Identity.Entities;

public class ProviderEntity : IHasConcurrencyStamp
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;
    public string Scheme { get; set; } = null!;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; set; }

    public virtual ICollection<UserExternalLoginEntity> ExternalLogins { get; set; } = new List<UserExternalLoginEntity>();
}
