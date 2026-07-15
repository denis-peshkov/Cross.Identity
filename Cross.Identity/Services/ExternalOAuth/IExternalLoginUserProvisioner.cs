namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Creates an in-app user profile after registration via an external OAuth provider.
/// </summary>
public interface IExternalLoginUserProvisioner
{
    Task ProvisionAsync(Guid userId, ExternalOAuth.ExternalOAuthProfile profile, CancellationToken cancellationToken);
}
