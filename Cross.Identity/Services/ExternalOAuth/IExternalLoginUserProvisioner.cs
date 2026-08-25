namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Creates an in-app user profile after registration via an external OAuth provider.
/// Optional host extension point registered in DI; stock library registration may omit it.
/// </summary>
public interface IExternalLoginUserProvisioner
{
    /// <summary>
    /// Provision application-specific data for a newly created (or resolved) local account
    /// after a successful OAuth complete.
    /// </summary>
    /// <param name="userAccountId">Local <c>UsersAccounts</c> id.</param>
    /// <param name="profile">Normalized provider profile for this login.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProvisionAsync(Guid userAccountId, ExternalOAuthProfile profile, CancellationToken cancellationToken);
}
