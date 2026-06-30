namespace Cross.Identity.ProcessEngine.Core.Forms;

/// <summary>
/// Description of a single form field.
/// </summary>
internal sealed record FieldDescriptor(
    string Key,
    FieldTypeEnum Type,
    bool Required = true,
    int? Min = null,
    int? Max = null,
    string? Regex = null
);
