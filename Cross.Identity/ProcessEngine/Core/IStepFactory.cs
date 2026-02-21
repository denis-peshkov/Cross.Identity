namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Фабрика построения шага из JSON-конфига и DI.
/// </summary>
public interface IStepFactory
{
    /// <summary>
    /// Тип (kind) шага: "collectForm", "sendCode", "verifyCode", ...
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Дефолтное вычисление kind по имени типа: VerifyCodeStepFactory -> "verifyCode"
    /// </summary>
    string GetKind => GetType().Name[..^"StepFactory".Length].ToCamelCase();

    /// <summary>
    /// Создать экземпляр шага из JSON-узла <paramref name="cfg"/>.
    /// </summary>
    IStep Create(JsonElement cfg, IServiceProvider sp);

}
