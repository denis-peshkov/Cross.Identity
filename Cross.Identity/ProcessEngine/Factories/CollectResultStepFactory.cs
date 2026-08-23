namespace Cross.Identity.ProcessEngine.Factories;

/// <summary>
/// Factory for <see cref="CollectResultStep"/>.
/// JSON parameters:
/// <list type="bullet">
///   <item><description><c>kind</c> — must be <c>"collectResult"</c>.</description></item>
///   <item><description><c>map</c> — projection object:
///       <c>"result_field": "bag_key"</c>. Key examples:
///       absolute <c>"token.AccessToken"</c> or relative <c>"Token"</c> (read as <c>"collectResult.Token"</c>).</description></item>
///   <item><description><c>resultKey</c> — (opt.) where to store the final dictionary; relative <c>"Result"</c> by default
///       → will be written as <c>"collectResult.Result"</c>.</description></item>
///   <item><description><c>next</c> — (opt.) next step name; <c>null</c> — finish.</description></item>
/// </list>
/// Example:
/// <code language="json">
/// {
///   "kind": "collectResult",
///   "map": {
///     "access_token": "token.AccessToken",
///     "user_id": "token.UserAccountId"
///   },
///   "next": null
/// }
/// </code>
/// </summary>
internal sealed class CollectResultStepFactory : IStepFactory
{
    /// <inheritdoc />
    public string Kind => ((IStepFactory)this).GetKind;

    /// <inheritdoc />
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        // map is required and must be an object
        if (!cfg.TryGetProperty("map", out var mapEl) || mapEl.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException("collectResult: 'map' object is required.");

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var p in mapEl.EnumerateObject())
        {
            // p.Name   -> result field
            // p.Value  -> Bag key (string)
            if (p.Value.ValueKind != JsonValueKind.String)
                throw new InvalidOperationException($"collectResult: map['{p.Name}'] must be a string (bag key).");

            map[p.Name] = p.Value.GetString()!;
        }

        var returnEmpty = cfg.TryGetProperty("returnEmpty", out var re) && re.ValueKind == JsonValueKind.True;

        return new CollectResultStep
        {
            Kind        = Kind,
            Map         = map,
            ReturnEmpty = returnEmpty,
            Next        = cfg.StrOpt("next")
        };
    }
}
