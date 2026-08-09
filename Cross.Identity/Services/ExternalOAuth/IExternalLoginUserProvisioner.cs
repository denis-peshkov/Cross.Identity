namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Creates an in-app user profile after registration via an external OAuth provider.
/// </summary>
public interface IExternalLoginUserProvisioner
{
    Task ProvisionAsync(Guid userId, ExternalOAuthProfile profile, CancellationToken cancellationToken);
}
