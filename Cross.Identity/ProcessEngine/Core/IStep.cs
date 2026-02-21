namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Базовый контракт шага процесса.
/// Каждый шаг имеет уникальное имя и выполняет свою логику,
/// оперируя <see cref="Bag"/> для передачи данных.
/// </summary>
public interface IStep
{
    /// <summary>
    /// Уникальное имя шага в рамках процесса.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Имя следующего шага (null — завершить процесс).
    /// </summary>
    string? Next { get; }

    /// <summary>
    /// Выполнить шаг.
    /// </summary>
    /// <param name="ctx">Контекст процесса (<see cref="Bag"/>).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see cref="StepResult"/> с результатом выполнения.</returns>
    ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken);
}
