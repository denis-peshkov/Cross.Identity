namespace Cross.Identity;

internal class FlowExecutor : IFlowExecutor
{
    private readonly IServiceProvider _sp;
    private readonly StepRegistry _registry;
    private readonly IProcessDefinitionProvider _definition;
    private readonly IRequestInput _requestInput;

    /// <summary>
    /// Creates the executor.
    /// </summary>
    /// <param name="sp">Root DI provider (needed to create scoped step dependencies).</param>
    /// <param name="registry">Process step factory registry.</param>
    /// <param name="definition">JSON process definition provider.</param>
    /// <param name="requestInput"></param>
    public FlowExecutor(
        IServiceProvider sp,
        StepRegistry registry,
        IProcessDefinitionProvider definition,
        IRequestInput requestInput)
    {
        _sp = sp;
        _registry = registry;
        _definition = definition;
        _requestInput = requestInput;
    }

    /// <inheritdoc/>
    public async Task<FlowResult> ExecuteAsync(Dictionary<string, object?> input, string flow, FlowOperationEnum operation, CancellationToken cancellationToken)
    {
        _sp.CheckLicense();

        // 1) Pass input payload to the engine (collectForm step reads it)
        _requestInput.Set(input);

        // 2) load process JSON
        var json = _definition.GetJson(flow, operation);

        // 3) create scope for step dependencies (IRequestInput, IUserService, ICodeService, IJwtIssuer, etc.)
        using var scope = _sp.CreateScope();

        // 3.1) Pass data into the new scope
        var scopedInput = scope.ServiceProvider.GetRequiredService<IRequestInput>();
        scopedInput.Set(input);

        // 4) build process from JSON
        var process = ProcessLoader.FromJson(json, _registry, scope.ServiceProvider);

        // 5) execute
        var bag = new Bag();
        await process.RunAsync(bag, cancellationToken).ConfigureAwait(false);

        // 6) return policy for "collectResult." prefix:
        //    - no collectResult.* -> Data = null
        //    - one or more collectResult.* -> Dictionary { field_name: value } (names without prefix)
        const string prefix = "collectResult.";
        var all = bag.ToDictionary();
        var resultPairs = all
            .Where(kv => kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        object? data;
        switch (resultPairs.Count)
        {
            case > 0:
                // multiple fields — return object { field : value } with prefix trimmed
                var trimmed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (k, v) in resultPairs)
                {
                    var name = k.Substring(prefix.Length); // "userId" from "collectResult.userId"
                    trimmed[name] = v;
                }
                data = trimmed;
                break;
            default:
                data = null;
                break;
        }

        return new FlowResult { Data = data };
    }
}
