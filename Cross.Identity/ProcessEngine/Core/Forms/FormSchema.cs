namespace Cross.Identity.ProcessEngine.Core.Forms;

/// <summary>
/// Form schema description (fields + inline validators).
/// IMPORTANT: the schema name (<see cref="Name"/>) is internal.
/// The Bag prefix is set by <b>step kind</b>, not by the schema name.
/// </summary>
internal sealed class FormSchema
{
    /// <summary>Arbitrary schema name (for debugging/logs/cache).</summary>
    public string Name { get; }

    /// <summary>List of form fields.</summary>
    public IReadOnlyList<FieldDescriptor> Fields { get; }

    /// <summary>Inline schema validators (for example, field comparison).</summary>
    public IReadOnlyList<IFormSchemaRule> Validators { get; }

    public FormSchema(
        string name,
        IEnumerable<FieldDescriptor> fields,
        IEnumerable<IFormSchemaRule>? validators = null)
    {
        Name       = name;
        Fields     = new List<FieldDescriptor>(fields);
        Validators = validators is null ? new List<IFormSchemaRule>() : new List<IFormSchemaRule>(validators);
    }
}
