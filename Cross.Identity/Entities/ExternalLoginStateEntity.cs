namespace Cross.Identity.Entities;

public class ExternalLoginStateEntity
{
    public long Id { get; set; }

    public string Nonce { get; set; } = null!;

    public string Provider { get; set; } = null!;

    public string? ReturnUrl { get; set; }

    public Guid? LinkUserId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
