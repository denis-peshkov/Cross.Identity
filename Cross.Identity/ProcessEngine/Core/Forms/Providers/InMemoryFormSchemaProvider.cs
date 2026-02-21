namespace Cross.Identity.ProcessEngine.Core.Forms.Providers;

/// <summary>
/// In-memory провайдер схем форм. Имена схем — уникальные строки,
/// напр. <c>"game.registration"</c>, <c>"shop.auth"</c>.
/// </summary>
public sealed class InMemoryFormSchemaProvider : IFormSchemaProvider
{
    private readonly Dictionary<string, FormSchema> _map = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Создать провайдер и зарегистрировать набор схем.</summary>
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
