namespace Cross.Identity.Services;

/// <summary>
/// Сервис работы с пользователями: создание, поиск и операции с паролем.
/// Используется шагами процесса:
/// <list type="bullet">
/// <item><description><c>CreateUserStep</c> — <see cref="CreateUserAsync"/></description></item>
/// <item><description><c>PasswordAuthStep</c> — <see cref="ValidatePasswordAsync"/></description></item>
/// <item><description><c>GetUserStep</c> — <see cref="GetUserIdByAsync"/></description></item>
/// </list>
/// </summary>
internal interface IUserService
{
    /// <summary>
    /// Найти идентификатор пользователя по полю-селектору.
    /// Допустимые значения <paramref name="selectorField"/> зависят от реализации
    /// (как минимум поддерживаются <c>"Email"</c>, <c>"UserName"</c>, <c>"Phone"</c>).
    /// </summary>
    /// <param name="selectorField">Имя поля, по которому ищем (напр., <c>"Email"</c>).</param>
    /// <param name="selectorValue">Значение селектора (напр., адрес email).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Строковый идентификатор пользователя или <c>null</c>, если не найден.</returns>
    Task<string> GetUserIdByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken);

    Task<UserAccountEntity> GetUserByAsync(string selectorField, string selectorValue, CancellationToken cancellationToken);

    /// <summary>
    /// Создать нового пользователя из плоской карты значений.
    /// Ключи карты — логические имена полей (напр., <c>"Email"</c>, <c>"UserName"</c>, <c>"Phone"</c>, <c>"Password"</c>).
    /// Необязательные ключи могут быть опущены.
    /// </summary>
    /// <param name="map">Карта полей пользователя.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Идентификатор созданного пользователя.</returns>
    Task<string> CreateUserAsync(IDictionary<string, object?> map, CancellationToken cancellationToken);

    /// <summary>
    /// Проверить корректность пароля пользователя, найденного по селектору.
    /// </summary>
    /// <param name="selectorField">Поле для поиска (напр., <c>"Email"</c>).</param>
    /// <param name="selectorValue">Значение селектора.</param>
    /// <param name="password">Проверяемый пароль.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><c>true</c>, если пароль верный; иначе <c>false</c>.</returns>
    Task<bool> ValidatePasswordAsync(string selectorField, string selectorValue, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Проверить корректность пароля пользователя, найденного по селектору.
    /// </summary>
    /// <param name="selectorField">Поле для поиска (напр., <c>"Email"</c>).</param>
    /// <param name="selectorValue">Значение селектора.</param>
    /// <param name="code">Проверяемый код.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><c>true</c>, если пароль верный; иначе <c>false</c>.</returns>
    Task<bool> ValidateCodeAsync(string selectorField, string selectorValue, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Установить (или заменить) пароль пользователя, найденного по селектору.
    /// </summary>
    /// <param name="selectorField">Поле для поиска (напр., <c>"Email"</c>).</param>
    /// <param name="selectorValue">Значение селектора.</param>
    /// <param name="newPassword">Новый пароль.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SetPasswordAsync(string selectorField, string selectorValue, string newPassword, CancellationToken cancellationToken);
}
