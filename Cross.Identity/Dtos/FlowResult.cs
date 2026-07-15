namespace Cross.Identity.Dtos;

/// <summary>
/// Generic result of a process (flow) execution.
/// </summary>
public sealed class FlowResult
{
    /// <summary>
    /// <c>collectResult</c> step data: dictionary <c>{ field_name: value }</c> (names from <c>map</c> in the JSON flow).
    /// <c>null</c> if the process has no <c>collectResult</c> step or it wrote no fields.
    /// </summary>
    public object? Data { get; init; }
}
