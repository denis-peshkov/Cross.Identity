namespace Cross.Identity.Services;

public interface IJwtTokenService
{
    /// <summary>
    /// Выпустить <c>id_token</c> (OIDC-подобный токен) на основе набора claims.
    /// </summary>
    /// <param name="claims">Список claims, которые должны войти в токен.</param>
    /// <returns>Строка токена в compact-формате.</returns>
    Task<string> GenerateIdTokenAsync(List<Claim> claims);

    /// <summary>
    /// Выпустить access-токен (JWT) для авторизации API.
    /// </summary>
    /// <param name="userId">ID пользователя.</param>
    /// <param name="familyId">ID семейства/контекста.</param>
    /// <param name="permissions">Разрешения, которые будут добавлены как claims.</param>
    /// <param name="claims">Дополнительные claims токена.</param>
    /// <returns>Строка access-токена в compact-формате.</returns>
    Task<string> GenerateAccessTokenAsync(Guid userId, Guid familyId, List<string> permissions, List<Claim> claims);

    /// <summary>
    /// Выпустить refresh-токен (JWT) для ротации сессии.
    /// </summary>
    /// <param name="userId">ID пользователя.</param>
    /// <param name="familyId">ID семейства/контекста.</param>
    /// <param name="claims">Дополнительные claims refresh-токена.</param>
    /// <returns>Строка refresh-токена.</returns>
    Task<string> GenerateRefreshTokenAsync(Guid userId, Guid familyId, List<Claim> claims);

    /// <summary>
    /// Проверка валидности access-токена по <c>jti</c>.
    /// Обычно применяется в контексте, где доступна исходная строка JWT и её можно безопасно распарсить.
    /// <para>
    /// Важно: для зашифрованных (JWE) токенов предпочтительнее использовать <see cref="ValidateAccessTokenJtiAsync"/>,
    /// т.к. middleware уже извлекает claims из токена.
    /// </para>
    /// </summary>
    /// <param name="accessToken">Строка access-токена (JWT/JWE) в формате compact.</param>
    /// <returns>
    /// <c>true</c>, если токен считается действительным (не отозван и не истёк по данным в БД),
    /// иначе <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenAsync(string accessToken);

    /// <summary>
    /// Проверка валидности access-токена по <c>jti</c>, без повторного парсинга/дешифрования токена.
    /// Используется в <c>JwtBearerEvents.OnTokenValidated</c>, когда middleware уже извлёк claims из токена.
    /// </summary>
    /// <param name="jti">JTI (идентификатор access-токена), извлечённый из JWT claims.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// <c>true</c>, если access-токен с данным <c>jti</c> присутствует в БД и не отозван, а также не истёк,
    /// иначе <c>false</c>.
    /// </returns>
    Task<bool> ValidateAccessTokenJtiAsync(Guid jti, CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверка валидности refresh-токена по его строке.
    /// Ожидает, что refresh-токен уже был выдан сервером и должен существовать в таблице refresh-токенов.
    /// </summary>
    /// <param name="refreshToken">Строка refresh-токена.</param>
    /// <returns><c>true</c>, если токен действителен (не отозван и не истёк), иначе <c>false</c>.</returns>
    Task<bool> ValidateRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Отозвать access-токен по его <c>jti</c> (перевести в revoked состояние в БД).
    /// </summary>
    /// <param name="jti">JTI (идентификатор) access-токена.</param>
    Task RevokeAccessTokenAsync(Guid jti);

    /// <summary>
    /// Очистка просроченных access-токенов из хранилища - удаление по <c>ExpiresAt</c>  (рекомендуется выполнять периодически).
    /// </summary>
    Task CleanupExpiredAccessTokensAsync();

    /// <summary>
    /// Получить значение claim из JWT по типу(типам).
    /// </summary>
    /// <param name="token">JWT в compact-формате.</param>
    /// <param name="claimTypes">
    /// Типы claims, по которым выполняется поиск. Первый найденный тип возвращается как значение.
    /// </param>
    /// <returns>Значение claim или <c>null</c>, если claim не найден.</returns>
    Task<string?> GetClaimValueAsync(string token, params string[] claimTypes);

    /// <summary>
    /// Получить refresh-токен из хранилища по его строковому значению.
    /// Внутри используется хэш токена, чтобы не хранить токен в открытом виде.
    /// </summary>
    /// <param name="refreshToken">Строка refresh-токена.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сущность refresh-токена или <c>null</c>, если токен не найден.</returns>
    Task<RefreshTokenEntity?> GetRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Время жизни access-токена в секундах.
    /// </summary>
    int AccessTokenExpiresInSeconds { get; }

    /// <summary>
    /// Инвалидировать (пометить как заменённый/отозванный) refresh-токен
    /// при ротации сессии.
    /// </summary>
    /// <param name="refreshToken">Текущий refresh-токен (строка), который нужно отозвать.</param>
    /// <param name="newJti">
    /// JTI нового refresh-токена, которым заменяется старый (используется для причин и связи).
    /// </param>
    /// <param name="cancellationToken">Токен отмены.</param>
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
