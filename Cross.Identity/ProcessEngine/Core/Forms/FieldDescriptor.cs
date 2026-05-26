namespace Cross.Identity.ProcessEngine.Core.Forms;

/// <summary>
/// Описание одного поля формы.
/// </summary>
internal sealed record FieldDescriptor(
    string Key,
    FieldTypeEnum Type,
    bool Required = true,
    int? Min = null,
    int? Max = null,
    string? Regex = null
);
