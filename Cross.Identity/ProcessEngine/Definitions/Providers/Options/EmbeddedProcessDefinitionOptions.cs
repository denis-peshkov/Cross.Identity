namespace Cross.Identity.ProcessEngine.Definitions.Providers.Options;

/// <summary>
/// Options for loading JSON definitions from embedded resources.
/// Used when registering the provider in DI.
/// </summary>
internal sealed class EmbeddedProcessDefinitionOptions
{
    /// <summary>
    /// Assembly name to read resources from (for example, "MyCompany.MyApp").
    /// </summary>
    public string? AssemblyName
    {
        get => Assembly?.GetName().Name;
        set => Assembly = !string.IsNullOrWhiteSpace(value)
            ? Assembly.Load(value)
            : Assembly.GetExecutingAssembly();
    }

    /// <summary>
    /// Assembly to read resources from.
    /// </summary>
    [JsonIgnore] // prevent serialization back to JSON
    public Assembly Assembly { get; set; } = Assembly.GetExecutingAssembly();

    /// <summary>
    /// Base namespace under which JSON resources are published.
    /// For example: <c>"MyCompany.MyApp.Flows.Definitions"</c>.
    /// </summary>
    public string BaseNamespace { get; set; } = null!;
}
