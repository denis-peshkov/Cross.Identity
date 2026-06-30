namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Base contract for a process step.
/// Each step has a unique name and runs its own logic,
/// operating on <see cref="Bag"/> to pass data.
/// </summary>
internal interface IStep
{
    /// <summary>
    /// Unique step name within the process.
    /// </summary>
    string Kind { get; }

    /// <summary>
    /// Next step name (null — finish the process).
    /// </summary>
    string? Next { get; }

    /// <summary>
    /// Execute the step.
    /// </summary>
    /// <param name="ctx">Process context (<see cref="Bag"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="StepResult"/> with the execution result.</returns>
    ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken);
}
