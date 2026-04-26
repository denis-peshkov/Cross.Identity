namespace Cross.Identity.Enums;

public enum RefreshTokenRevokeReason : short
{
    #region 1. Security-причины (критичные)

    /// <summary>
    /// Подача старого токена после ротации → попытка атаки → инвалидируем всю “семью”.
    /// </summary>
    REPLAY_DETECTED,

    /// <summary>
    /// Выявлено использование с другого устройства / IP сочетания → маркер кражи.
    /// </summary>
    /// <remarks>
    /// Обычно используется вместе с аналитикой:
    /// • слишком много попыток с разных IP
    /// • fingerprint mismatch
    /// • suspicious geo-location
    /// </remarks>
    TOKEN_STOLEN,

    /// <summary>
    /// Хеш устройства (DeviceFingerprint) поменялся → токен украден или подделан.
    /// </summary>
    DEVICE_MISMATCH,

    IP_MISMATCH,

    /// <summary>
    /// Некоторые системы хардкорно проверяют регион за region-lock.
    /// </summary>
    LOCATION_MISMATCH,

    /// <summary>
    /// User-Agent сильно отличается → возможный вор.
    /// </summary>
    USER_AGENT_MISMATCH,

    #endregion

    #region 2. Business-Security причины (поведение пользователя). Эти причины связаны с условиями работы или ограничениями.

    /// <summary>
    /// Пользователь сменил пароль → ВСЕ refresh tokens ревокаются.
    /// </summary>
    PASSWORD_CHANGED,

    /// <summary>
    /// Пользователь сменил/отвязал MFA → все токены становятся недействительными.
    /// </summary>
    MFA_RESET,

    /// <summary>
    /// Аномалия: много логинов, много ошибок, аномальная активность.
    /// </summary>
    SUSPICIOUS_ACTIVITY,

    /// <summary>
    /// Сессия была валидна X дней → автоматически ревокнуть FamilyId. Например: максимум 30 дней, независимо от активности.
    /// </summary>
    SESSION_EXPIRED,

    #endregion

    #region 3. User-initiated (пользователь сам)

    /// <summary>
    /// Пользователь нажал Logout → токен/семейство ревокаются.
    /// </summary>
    USER_LOGOUT,

    /// <summary>
    /// Пользователь нажал “Logout from all devices”.
    /// </summary>
    USER_LOGOUT_ALL,

    /// <summary>
    /// Пользователь открепил устройство в разделе “Мои устройства”.
    /// </summary>
    DEVICE_REMOVED_BY_USER,

    #endregion

    #region 4. Admin / backend-initiated причины

    /// <summary>
    /// Администратор вручную отключил пользователя / устройство / токены.
    /// </summary>
    ADMIN_REVOKE,

    /// <summary>
    /// Учетка заблокирована — revoke всех токенов.
    /// </summary>
    ACCOUNT_DISABLED,

    /// <summary>
    /// Удалена учетная запись.
    /// </summary>
    ACCOUNT_DELETED,

    #endregion

    #region 5. Technical причины

    /// <summary>
    /// Детектор безопасности считает токен компрометированным (AI/ML, anti-fraud).
    /// </summary>
    TOKEN_COMPROMISED,

    /// <summary>
    /// Токен подпорчен, неправильная подпись, просрочен, неверный audience.
    /// </summary>
    TOKEN_FORMAT_INVALID,

    /// <summary>
    /// Изменена схема токена / алгоритм / версия → старые токены невалидны.
    /// </summary>
    /// <remarks>
    /// Например:
    /// • переход с HS256 → RS256
    /// • смена pepper
    /// • смена структуры payload
    /// </remarks>
    TOKEN_UPGRADE_REQUIRED,

    /// <summary>
    /// Forcing rotation (например, через флаг в БД) — иногда используется при миграциях.
    /// </summary>
    ROTATION_REQUIRED,

    #endregion
}
