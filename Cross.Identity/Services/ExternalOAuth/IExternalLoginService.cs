namespace Cross.Identity.Services.ExternalOAuth;

internal interface IExternalLoginService
{
    Task<string> InitiateAsync(
        string provider,
        string? returnUrl,
        Guid? linkUserId,
        CancellationToken cancellationToken);

    Task<Guid> CompleteAsync(
        string code,
        string state,
        string? error,
        string? errorDescription,
        CancellationToken cancellationToken);
}
