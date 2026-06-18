namespace Cross.Identity.Services.ExternalOAuth;

internal sealed class ExternalLoginStatePayload
{
    public string Nonce { get; init; }

    public string Provider { get; init; }

    public string? ReturnUrl { get; init; }

    public Guid? LinkUserId { get; init; }
}
