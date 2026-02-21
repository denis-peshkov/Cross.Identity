namespace Cross.Identity.Services;

public interface IJwtTokenService
{
    Task<string> GenerateIdTokenAsync(List<Claim> claims);

    /// <summary>
    /// Выпустить JWT на основе набора клеймов и времени жизни.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="familyId"></param>
    /// <param name="permissions"></param>
    /// <param name="claims">
    /// Карта клеймов. Значения могут быть строкой или коллекцией строк.
    /// Обязательные клеймы (<c>sub</c>, <c>iat</c>, <c>exp</c>) добавляются реализацией автоматически,
    /// исходя из настроек и параметра <paramref name="lifetime"/>.
    /// </param>
    /// <returns>Подписанный JWT в компактной сериализации.</returns>
    Task<string> GenerateAccessTokenAsync(Guid userId, Guid familyId, List<string> permissions, List<Claim> claims);

    /// <summary>
    /// Выпустить Refresh JWT token на основе времени жизни.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="familyId"></param>
    /// <param name="claims"></param>
    /// <returns></returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, Guid familyId, List<Claim> claims);

    /// <summary>
    /// Проверка валидности токена по jti (например в JwtBearerEvents)
    /// </summary>
    /// <param name="accessToken"></param>
    /// <returns></returns>
    Task<bool> ValidateAccessTokenAsync(string accessToken);

    Task<bool> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Метод для отзыва токена
    /// </summary>
    /// <param name="jti"></param>
    Task RevokeAccessTokenAsync(Guid jti);

    /// <summary>
    /// Очистка просроченных access-токенов (рекомендуется выполнять периодически)
    /// </summary>
    Task CleanupExpiredAccessTokensAsync();

    Task<string?> GetClaimValueAsync(string token, params string[] claimTypes);

    Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    int AccessTokenExpiresInSeconds { get; }

    Task InvalidateRefreshTokenAsync(string refreshToken, string newJti, CancellationToken cancellationToken);
}

// /// <summary>
// /// Сервис выпуска JSON Web Token (JWT).
// /// Используется шагом <c>IssueJwtStep</c>.
// /// </summary>
// public interface IJwtIssuer
// {
//     /// <summary>
//     /// Выпустить JWT на основе набора клеймов и времени жизни.
//     /// </summary>
//     /// <param name="claims">
//     /// Карта клеймов. Значения могут быть строкой или коллекцией строк.
//     /// Обязательные клеймы (<c>sub</c>, <c>iat</c>, <c>exp</c>) добавляются реализацией автоматически,
//     /// исходя из настроек и параметра <paramref name="lifetime"/>.
//     /// </param>
//     /// <param name="lifetime">Время жизни токена (TTL).</param>
//     /// <returns>Подписанный JWT в компактной сериализации.</returns>
//     string Issue(IDictionary<string, object> claims, TimeSpan lifetime);
// }
