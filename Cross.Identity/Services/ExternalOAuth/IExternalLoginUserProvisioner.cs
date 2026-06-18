namespace Cross.Identity.Services.ExternalOAuth;

/// <summary>
/// Создаёт профиль пользователя в приложении после регистрации через внешний OAuth-провайдер.
/// </summary>
public interface IExternalLoginUserProvisioner
{
    Task ProvisionAsync(Guid userId, ExternalOAuth.ExternalOAuthProfile profile, CancellationToken cancellationToken);
}
