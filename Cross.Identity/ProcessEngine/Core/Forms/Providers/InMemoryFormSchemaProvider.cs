namespace Cross.Identity.ProcessEngine.Core.Forms.Providers;

/// <summary>
/// In-memory form schema provider. Schema names are unique strings,
/// e.g. <c>"main.register"</c>, <c>"main.token"</c>.
/// </summary>
internal sealed class InMemoryFormSchemaProvider : IFormSchemaProvider
{
    private readonly Dictionary<string, FormSchema> _map = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Create the provider and register a set of schemas.</summary>
    public InMemoryFormSchemaProvider(IEnumerable<FormSchema> schemas)
    {
        foreach (var s in schemas) _map[s.Name] = s;
    }

    /// <inheritdoc/>
    public FormSchema Get(string name) =>
        _map.TryGetValue(name, out var s)
            ? s
            : throw new KeyNotFoundException($"Form schema '{name}' not found.");
}
