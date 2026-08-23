namespace Cross.Identity.ProcessEngine.Steps;

/// <summary>
/// Step that aggregates a result from <see cref="Bag"/> using a field map and publishes
/// the resulting dictionary into a single key (by default — <c>"{Kind}.Result"</c>).
/// <para>
/// Performs no validation: missing values are simply skipped.
/// </para>
/// Usage example (see the factory for logic):
/// <code language="json">
/// {
///   "kind": "collectResult",
///   "map": {
///     "access_token": "token.AccessToken",
///     "user_account_id": "token.UserAccountId"
///   },
///   "next": null
/// }
/// Result example:
/// "collectResult.access_token", "collectResult.user_account_id".
/// </code>
/// </summary>
internal sealed class CollectResultStep : IStep
{
    /// <inheritdoc />
    public required string Kind { get; init; } = "collectResult";

    /// <inheritdoc/>
    public string? Next { get; init; }

    /// <summary>
    /// Map of "result field name" → "key in <see cref="Bag"/>".
    /// The value key may be absolute (<c>"step.Field"</c>) or relative
    /// (then qualified as <c>"{Kind}.Field"</c>).
    /// </summary>
    public required IReadOnlyDictionary<string, string> Map { get; init; }

    /// <summary>
    /// When true, the step explicitly indicates there is no data to return
    /// (FlowExecutor returns Data = null).
    /// </summary>
    public bool ReturnEmpty { get; init; }

    /// <inheritdoc />
    public ValueTask<StepResult> ExecuteAsync(Bag ctx, CancellationToken cancellationToken)
    {
        if (ReturnEmpty)
        {
            ctx.Set($"{Kind}._empty", true);
            return ValueTask.FromResult(StepResult.Ok(Next));
        }

        foreach (var (outField, bagKeyRaw) in Map)
        {
            // Relative key → "{Kind}.{bagKeyRaw}"
            var bagKey = BagKey.Qualify(Kind, bagKeyRaw);

            if (ctx.TryGet<object?>(bagKey, out var value))
            {
                ctx.Set($"{Kind}.{outField}", value);
            }
            else
            {
                // no validation: simply skip missing keys
                // To store null instead, uncomment:
                // output[outField] = null;
            }
        }

        return ValueTask.FromResult(StepResult.Ok(Next));
    }
}
