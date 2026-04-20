namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Источник JSON-дефиниций динамических процессов (flows).
/// Дефиниции хранятся по ключу <c>{flow}.{operation}</c> и отдаются как строка JSON
/// с корнем вида:
/// <code>
/// {
///   "start": "stepName",
///   "steps": [ { "kind": "...", ... }, ... ]
/// }
/// </code>
/// </summary>
internal interface IProcessDefinitionProvider
{
    /// <summary>
    /// Получить JSON-дефиницию процесса по идентификаторам флоу и операции.
    /// </summary>
    /// <param name="flow">Идентификатор флоу (например, <c>"game"</c>, <c>"licenses"</c>, <c>"shop"</c>).</param>
    /// <param name="operation">
    /// Идентификатор операции (свободный слаг, например <c>"register"</c>, <c>"auth"</c>, <c>"getuser"</c>,
    /// <c>"request-code"</c>, <c>"reset-password"</c> и т.п.).
    /// </param>
    /// <returns>Строка JSON-дефиниции процесса.</returns>
    /// <exception cref="KeyNotFoundException">Если дефиниция не найдена.</exception>
    string GetJson(string flow, FlowOperationEnum operation);

    /// <summary>
    /// Получить текстовый шаблон из embedded-ресурсов.
    /// Конвенция имени ресурса: <c>{BaseNamespace}.Templates.{name}.{languageCode}.{format}</c>.
    /// </summary>
    /// <param name="name">Имя шаблона (например, "verify-code", "reset-password").</param>
    /// <param name="languageCode">Языковой код (например, "en", "ru", "ro-RO").</param>
    /// <param name="format">Формат ("txt" или "html").</param>
    /// <returns>Содержимое шаблона как строка.</returns>
    /// <exception cref="KeyNotFoundException">Если шаблон не найден.</exception>
    string GetTemplate(string name, string languageCode, string format);
}
