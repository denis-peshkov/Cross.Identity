namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Convenience builder for declaratively assembling a process in code (alternative to JSON definitions).
/// Requirement: each step <c>Kind</c> is unique within a process (case-insensitive).
/// </summary>
internal sealed class ProcessBuilder
{
    private readonly List<IStep> _steps = new();
    private readonly HashSet<string> _kinds = new(StringComparer.OrdinalIgnoreCase);
    private string? _start;

    /// <summary>
    /// Set the start step and add it to the process.
    /// </summary>
    public ProcessBuilder StartWith(IStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        AddInternal(step, isStart: true);
        return this;
    }

    /// <summary>
    /// Add the next step (order matters only for readability;
    /// actual transitions are defined by the steps via their <c>Next</c>).
    /// </summary>
    public ProcessBuilder Then(IStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        AddInternal(step, isStart: false);
        return this;
    }

    /// <summary>
    /// Build the process. Throws if start is not set
    /// or the step list is empty.
    /// </summary>
    public ProcessExecutor Build()
    {
        if (_start is null)
            throw new InvalidOperationException("Start step is not set. Call StartWith(step) first.");
        if (_steps.Count == 0)
            throw new InvalidOperationException("No steps added to the process.");

        // Extra sanity checks
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
