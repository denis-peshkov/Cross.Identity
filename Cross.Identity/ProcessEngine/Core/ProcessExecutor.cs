namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Process executor: holds a step map (by <c>Kind</c>) and follows transitions via <c>Next</c>.
/// </summary>
internal sealed class ProcessExecutor
{
    private readonly Dictionary<string, IStep> _steps;
    private readonly string _start;

    /// <summary>
    /// Internal constructor. Assumes <c>Kind</c> uniqueness and <c>_start</c> validity
    /// were verified during process loading (see <see cref="ProcessLoader.FromJson"/>).
    /// </summary>
    internal ProcessExecutor(string start, IEnumerable<IStep> steps)
    {
        // Map steps by Kind case-insensitively (same as ProcessLoader).
        _steps = steps.ToDictionary(s => s.Kind, StringComparer.OrdinalIgnoreCase);
        _start = start;
    }

    /// <summary>
    /// Start process execution.
    /// </summary>
    /// <param name="ctx">Data context (<see cref="Bag"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a step with the specified <c>kind</c> is not found or a <c>Next</c> transition points to a missing step.
    /// </exception>
    public async Task RunAsync(Bag ctx, CancellationToken cancellationToken)
    {
        string? current = _start;

        while (current is not null)
        {
            if (!_steps.TryGetValue(current, out var step))
                throw new InvalidOperationException(
                    $"Step '{current}' not found.");

            var result = await step.ExecuteAsync(ctx, cancellationToken).ConfigureAwait(false);

            if (result.Status == StepStatusEnum.Fail)
                throw result.Error!; // step error is propagated upward

            // null => finish the process
            if (result.Next is null)
                return;

            // Move to the next step by its kind
            if (!_steps.ContainsKey(result.Next))
                throw new InvalidOperationException(
                    $"Next step '{result.Next}' (from '{current}') not found.");

            current = result.Next;
        }
    }
}
