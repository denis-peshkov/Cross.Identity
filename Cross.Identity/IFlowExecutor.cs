namespace Cross.Identity;

public interface IFlowExecutor
{
    /// <summary>
    /// Runs the specified process and returns its execution result.
    /// </summary>
    /// <param name="input">Scoped HTTP request input for form-collection steps. Controller/endpoint sets the request body; <c>CollectFormStep</c> reads it.</param>
    /// <param name="flow">Flow identifier (e.g. "game"). Examples: <c>"game"</c>, <c>"licenses"</c>, <c>"shop"</c>.</param>
    /// <param name="operation">Operation identifier within the flow (e.g. "auth"). Examples: <c>"register"</c>, <c>"auth"</c>, <c>"getuser"</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FlowResult> ExecuteAsync(Dictionary<string, object?> input, string flow, FlowOperationEnum operation, CancellationToken cancellationToken);
}
