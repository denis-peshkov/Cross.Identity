namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Process builder from a JSON definition: <c>start</c> field + <c>steps</c> array.
/// Each step contains <c>kind</c> and parameters; step creation is delegated to <see cref="StepRegistry"/>.
/// </summary>
internal static class ProcessLoader
{
    /// <summary>
    /// Build an executable process from a JSON string.
    /// JSON requirements:
    /// <code language="json">
    /// {
    ///   "start": "collectForm",
    ///   "steps": [
    ///     { "kind": "collectForm", ... },
    ///     { "kind": "passwordAuth", ... },
    ///     { "kind": "issueJwt", ... }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    /// <param name="json">Process JSON definition.</param>
    /// <param name="reg">Step factory registry.</param>
    /// <param name="sp">DI provider for resolving step dependencies.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>start</c> is missing, the <c>steps</c> array is missing, steps duplicate <c>kind</c>,
    /// or <c>start</c> does not match any step.
    /// </exception>
    public static ProcessExecutor FromJson(string json, StepRegistry reg, IServiceProvider sp)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        ArgumentNullException.ThrowIfNull(reg);
        ArgumentNullException.ThrowIfNull(sp);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // 1) start
        if (!root.TryGetProperty("start", out var startEl) || startEl.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Process JSON must contain string property 'start'.");

        var start = startEl.GetString();
        if (string.IsNullOrWhiteSpace(start))
            throw new InvalidOperationException("'start' must be a non-empty string (step kind).");

        // 2) steps[]
        if (!root.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Process JSON must contain array 'steps'.");

        var steps = new List<IStep>();
        var kinds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var stepJson in stepsEl.EnumerateArray())
        {
            // Delegate step creation to the registry — it extracts kind and invokes the factory.
            var step = reg.Create(stepJson, sp);

            // Ensure kind is unique within a single process
            if (!kinds.Add(step.Kind))
                throw new InvalidOperationException($"Duplicate step kind '{step.Kind}' in process. Each kind must be unique within a flow.");

            steps.Add(step);
        }

        // 3) Validation: start must point to an existing step
        if (!kinds.Contains(start!))
            throw new InvalidOperationException($"Start refers to unknown step kind '{start}'. Ensure there is a step with this kind in 'steps'.");

        // 4) Build the executable process
        return new ProcessExecutor(start!, steps);
    }
}
