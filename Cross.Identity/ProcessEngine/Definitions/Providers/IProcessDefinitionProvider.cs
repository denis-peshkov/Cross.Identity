namespace Cross.Identity.ProcessEngine.Definitions.Providers;

/// <summary>
/// Source of JSON definitions for dynamic processes (flows).
/// Definitions are stored under key <c>{flow}.{operation}</c> and returned as a JSON string
/// with a root like:
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
    /// Get a process JSON definition by flow and operation identifiers.
    /// </summary>
    /// <param name="flow">Flow identifier (for example, <c>"game"</c>, <c>"licenses"</c>, <c>"shop"</c>).</param>
    /// <param name="operation">
    /// Operation identifier (free-form slug, for example <c>"register"</c>, <c>"auth"</c>, <c>"getuser"</c>,
    /// <c>"request-code"</c>, <c>"reset-password"</c>, etc.).
    /// </param>
    /// <returns>Process JSON definition string.</returns>
    /// <exception cref="KeyNotFoundException">When the definition is not found.</exception>
    string GetJson(string flow, FlowOperationEnum operation);

    /// <summary>
    /// Get a text template from embedded resources.
    /// Resource naming convention: <c>{BaseNamespace}.Templates.{name}.{languageCode}.{format}</c>.
    /// </summary>
    /// <param name="name">Template name (for example, "verify-code", "reset-password").</param>
    /// <param name="languageCode">Language code (for example, "en", "ru", "ro-RO").</param>
    /// <param name="format">Format ("txt" or "html").</param>
    /// <returns>Template contents as a string.</returns>
    /// <exception cref="KeyNotFoundException">When the template is not found.</exception>
    string GetTemplate(string name, string languageCode, string format);
}
