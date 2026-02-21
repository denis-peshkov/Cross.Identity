namespace Cross.Identity.Dtos;

/// <summary>
/// Универсальный результат выполнения процесса (flow).
/// </summary>
public sealed class FlowResult
{
    /// <summary>
    /// Данные, собранные шагом <c>collectResult</c>,
    /// либо весь <see cref="Bag"/>, если такого шага не было в процессе.
    /// </summary>
    public object? Data { get; init; }
}
