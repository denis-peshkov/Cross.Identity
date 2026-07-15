namespace Cross.Identity.ProcessEngine.Core;

/// <summary>
/// Step factory registry. Creates a step by its <c>kind</c>.
/// <para>
/// Thread safety: initialization at application startup (DI) is assumed,
/// followed by read-only access. Wrap with synchronization if registering at runtime.
/// </para>
/// </summary>
internal sealed class StepRegistry
{
    private readonly Dictionary<string, IStepFactory> _steps = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Create an empty registry.</summary>
    public StepRegistry() { }

    /// <summary>Create a registry and register a set of factories.</summary>
    public StepRegistry(IEnumerable<IStepFactory> factories)
    {
        foreach (var f in factories)
            Register(f);
    }

    /// <summary>Register a factory. The latest entry with the same <c>Kind</c> overwrites the previous one.</summary>
    public void Register(IStepFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (string.IsNullOrWhiteSpace(factory.Kind))
            throw new ArgumentException("Factory.Kind must be a non-empty string.", nameof(factory));

        _steps[factory.Kind] = factory;
    }

    /// <summary>Bulk factory registration.</summary>
    public void RegisterRange(IEnumerable<IStepFactory> factories)
    {
        foreach (var f in factories)
            Register(f);
    }

    /// <summary>
    /// Create a step of the specified <paramref name="kind"/> type.
    /// <para>
    /// Note: the factory sets <c>step.Kind = factory.Kind</c> itself.
    /// Any <c>kind</c> field in JSON is used only for routing to the factory/validation
    /// and must not be assigned on the step.
    /// </para>
    /// </summary>
    public IStep Create(string kind, JsonElement cfg, IServiceProvider sp)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        ArgumentNullException.ThrowIfNull(sp);

        if (!_steps.TryGetValue(kind, out var factory))
            throw new InvalidOperationException($"Unknown step kind '{kind}'.");

        // the factory may additionally validate that cfg.kind (if present) matches factory.Kind
        return factory.Create(cfg, sp);
    }

    /// <summary>
    /// Create a step directly from a step JSON config, extracting <c>kind</c> from <paramref name="cfg"/>.
    /// </summary>
    public IStep Create(JsonElement cfg, IServiceProvider sp)
    {
        if (!cfg.TryGetProperty("kind", out var k) || k.ValueKind != JsonValueKind.String)
            throw new InvalidOperationException("Step 'kind' is required in step JSON.");

        var kind = k.GetString()!;

        return Create(kind, cfg, sp);
    }

    /// <summary>Check whether a factory is registered for the specified <paramref name="kind"/>.</summary>
    public bool Has(string kind)
        => _steps.ContainsKey(kind);

    /// <summary>List of available <c>kind</c> values.</summary>
    public IReadOnlyCollection<string> Kinds
        => _steps.Keys.ToArray();

    /// <summary>Clear the registry (use with caution).</summary>
    public void Clear()
        => _steps.Clear();
}
