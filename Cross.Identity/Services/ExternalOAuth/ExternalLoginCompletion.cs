namespace Cross.Identity.Services.ExternalOAuth;

internal sealed record ExternalLoginCompletion(Guid UserId, bool IsLinking);
