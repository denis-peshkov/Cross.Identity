namespace Cross.Identity.Dtos;

/// <summary>
/// Универсальный результат выполнения процесса (flow).
/// </summary>
public sealed class FlowResult
{
    /// <summary>
    /// Данные шага <c>collectResult</c>: словарь <c>{ имя_поля: значение }</c> (имена из <c>map</c> в JSON flow).
    /// Если в процессе нет шага <c>collectResult</c> или он не записал полей — <c>null</c>.
    /// </summary>
    public object? Data { get; init; }
}
