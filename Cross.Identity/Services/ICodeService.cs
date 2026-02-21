namespace Cross.Identity.Services;

/// <summary>
/// Сервис одноразовых кодов (OTP): отправка и проверка.
/// Используется шагами:
/// <list type="bullet">
/// <item><description><c>SendCodeStep</c> — <see cref="SendAsync"/></description></item>
/// <item><description><c>VerifyCodeStep</c> и <c>CodeAuthStep</c> — <see cref="VerifyAsync"/></description></item>
/// </list>
/// </summary>
internal interface ICodeService
{
    /// <summary>
    /// Отправить одноразовый код на указанный канал/адрес с временем жизни.
    /// </summary>
    /// <param name="channel">Канал доставки (напр., <c>"email"</c> или <c>"phone"</c>).</param>
    /// <param name="destination">Назначение (напр., адрес email или номер телефона).</param>
    /// <param name="code">Текст кода (генерируется вне сервиса или самим сервисом — на усмотрение архитектуры).</param>
    /// <param name="msg"></param>
    /// <param name="userId"></param>
    /// <param name="ttl">Время жизни кода.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task SendAsync(NotificationMessage msg, string code, string userId, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>
    /// Проверить одноразовый код для указанного канала и идентичности.
    /// </summary>
    /// <param name="channel">Канал (напр., <c>"email"</c>/<c>"phone"</c>).</param>
    /// <param name="identity">Идентичность (адрес email, телефон и т.п.).</param>
    /// <param name="code">Предъявленный код.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><c>true</c>, если код валиден и не истёк; иначе <c>false</c>.</returns>
    Task<bool> VerifyAsync(string channel, string identity, string code, CancellationToken cancellationToken);
}
