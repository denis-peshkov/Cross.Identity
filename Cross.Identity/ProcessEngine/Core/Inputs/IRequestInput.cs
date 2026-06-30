namespace Cross.Identity.ProcessEngine.Core.Inputs;

/// <summary>
/// Scoped HTTP request input provider for form collection steps.
/// The controller/endpoint stores the request body; <c>CollectFormStep</c> reads it.
/// </summary>
internal interface IRequestInput
{
    /// <summary>Get request data.</summary>
    Task<IDictionary<string, object?>> GetAsync(CancellationToken cancellation);

    /// <summary>Set request data (usually from the controller).</summary>
    void Set(IDictionary<string, object?> data);
}
