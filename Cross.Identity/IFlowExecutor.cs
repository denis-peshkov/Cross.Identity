namespace Cross.Identity;

public interface IFlowExecutor
{
    /// <summary>
    /// Runs the specified process and returns its execution result.
    /// </summary>
    /// <param name="input">Scoped HTTP request input for form-collection steps. Controller/endpoint sets the request body; <c>CollectFormStep</c> reads it.</param>
    /// <param name="flow">flow identifier (e.g. <c>"main"</c>).</param>
    /// <param name="operation">Operation within the flow (e.g. <see cref="FlowOperationEnum.Register"/>, <see cref="FlowOperationEnum.Token"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<FlowResult> ExecuteAsync(Dictionary<string, object?> input, string flow, FlowOperationEnum operation, CancellationToken cancellationToken);
}
