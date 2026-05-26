namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Исполнитель процесса: хранит карту шагов (по <c>Kind</c>) и выполняет переходы по <c>Next</c>.
/// </summary>
internal sealed class ProcessExecutor
{
    private readonly Dictionary<string, IStep> _steps;
    private readonly string _start;

    /// <summary>
    /// Внутренний конструктор. Предполагается, что уникальность <c>Kind</c> и валидность <c>_start</c>
    /// проверены на этапе загрузки процесса (см. <see cref="ProcessLoader.FromJson"/>).
    /// </summary>
    internal ProcessExecutor(string start, IEnumerable<IStep> steps)
    {
        // Сопоставляем шаги по Kind без учета регистра (как делали в ProcessLoader).
        _steps = steps.ToDictionary(s => s.Kind, StringComparer.OrdinalIgnoreCase);
        _start = start;
    }

    /// <summary>
    /// Запустить выполнение процесса.
    /// </summary>
    /// <param name="ctx">Контекст данных (<see cref="Bag"/>).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <exception cref="InvalidOperationException">
    /// Бросается, если шаг с указанным <c>kind</c> не найден либо переход <c>Next</c> указывает на несуществующий шаг.
    /// </exception>
    public async Task RunAsync(Bag ctx, CancellationToken cancellationToken = default)
    {
        string? current = _start;

        while (current is not null)
        {
            if (!_steps.TryGetValue(current, out var step))
                throw new InvalidOperationException(
                    $"Step '{current}' not found.");

            var result = await step.ExecuteAsync(ctx, cancellationToken).ConfigureAwait(false);

            if (result.Status == StepStatusEnum.Fail)
                throw result.Error!; // ошибка шага пробрасывается наверх

            // null => завершить процесс
            if (result.Next is null)
                return;

            // Переходим к следующему шагу по его kind
            if (!_steps.ContainsKey(result.Next))
                throw new InvalidOperationException(
                    $"Next step '{result.Next}' (from '{current}') not found.");

            current = result.Next;
        }
    }
}
