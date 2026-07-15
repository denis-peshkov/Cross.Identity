namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Factory for building a step from a JSON config and DI.
/// </summary>
internal interface IStepFactory
{
    /// <summary>
    /// Step type (kind): "collectForm", "sendCode", "verifyCode", ...
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Default kind derived from the type name: VerifyCodeStepFactory -> "verifyCode"
    /// </summary>
    string GetKind => GetType().Name[..^"StepFactory".Length].ToCamelCase();

    /// <summary>
    /// Create a step instance from JSON node <paramref name="cfg"/>.
    /// </summary>
    IStep Create(JsonElement cfg, IServiceProvider sp);

}
