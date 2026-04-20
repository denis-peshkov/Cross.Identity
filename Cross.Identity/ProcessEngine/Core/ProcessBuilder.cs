namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Удобный билдер для декларативной сборки процесса кодом (альтернатива JSON-определениям).
/// Требование: в одном процессе каждый <c>Kind</c> шага уникален (без учёта регистра).
/// </summary>
internal sealed class ProcessBuilder
{
    private readonly List<IStep> _steps = new();
    private readonly HashSet<string> _kinds = new(StringComparer.OrdinalIgnoreCase);
    private string? _start;

    /// <summary>
    /// Задать стартовый шаг и добавить его в процесс.
    /// </summary>
    public ProcessBuilder StartWith(IStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        AddInternal(step, isStart: true);
        return this;
    }

    /// <summary>
    /// Добавить следующий шаг (порядок важен только для читаемости;
    /// фактические переходы определяются самими шагами через их <c>Next</c>).
    /// </summary>
    public ProcessBuilder Then(IStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        AddInternal(step, isStart: false);
        return this;
    }

    /// <summary>
    /// Собрать процесс. Бросает исключение, если старт не задан
    /// или список шагов пуст.
    /// </summary>
    public ProcessExecutor Build()
    {
        if (_start is null)
            throw new InvalidOperationException("Start step is not set. Call StartWith(step) first.");
        if (_steps.Count == 0)
            throw new InvalidOperationException("No steps added to the process.");

        // Дополнительные проверки «на всякий случай»
        if (!_kinds.Contains(_start))
            throw new InvalidOperationException($"Start refers to unknown step kind '{_start}'.");

        return new ProcessExecutor(_start, _steps);
    }

    private void AddInternal(IStep step, bool isStart)
    {
        if (string.IsNullOrWhiteSpace(step.Kind))
            throw new ArgumentException("Step.Kind must be a non-empty string.", nameof(step));

        if (!_kinds.Add(step.Kind))
            throw new InvalidOperationException($"Duplicate step kind '{step.Kind}' in process. Each kind must be unique within a flow.");

        _steps.Add(step);

        if (isStart)
            _start = step.Kind;
    }
}
