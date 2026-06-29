namespace Cross.Identity.Options;

/// <summary>
/// Настройки external OAuth (<c>Authentication:ExternalLogin</c>).
/// <para>
/// <c>StateLifetime</c> задаёт TTL строки в таблице <c>auth.ExternalLoginStates</c>
/// (<see cref="Entities.ExternalLoginStateEntity"/>), куда пишет <c>ExternalLoginService</c>:
/// <c>InitiateAsync</c> — insert; <c>ResolveStateAsync</c> (из <c>CompleteAsync</c>) — select и delete.
/// </para>
/// </summary>
public sealed class ExternalLoginOptions
{
    public const string SectionName = "Authentication:ExternalLogin";

    /// <summary>
    /// Полный URL SPA-callback, зарегистрированный у OAuth-провайдера.
    /// Env: <c>AUTH_EXTERNAL_LOGIN_CALLBACK_URL</c> → <c>Authentication__ExternalLogin__CallbackUrl</c>.
    /// </summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Время жизни OAuth state в БД (<c>ExternalLoginStates.ExpiresAt</c>).
    /// </summary>
    public TimeSpan StateLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Credentials провайдеров — только из env / user-secrets, не из appsettings.
    /// Пример: <c>Authentication__ExternalLogin__Providers__Google__ClientId</c>.
    /// </summary>
    public Dictionary<string, ExternalLoginProviderOptions> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ExternalLoginProviderOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public bool IsConfigured =>
        IsEnabled
        && !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret);
}
